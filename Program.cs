using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.RateLimiting;
using Kafo.Web.Configuration;
using Kafo.Web.Data;
using Kafo.Web.Middleware;
using Kafo.Web.Security;
using Kafo.Web.Services.Implementations;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
var logPath = ResolveConfiguredPath(
    builder.Environment.ContentRootPath,
    builder.Configuration["Security:LogPath"],
    Path.Combine(appDataPath, "logs"));
var dataProtectionKeysPath = ResolveConfiguredPath(
    builder.Environment.ContentRootPath,
    builder.Configuration["Security:DataProtectionKeysPath"],
    Path.Combine(appDataPath, "data-protection-keys"));
Directory.CreateDirectory(appDataPath);
Directory.CreateDirectory(logPath);
Directory.CreateDirectory(dataProtectionKeysPath);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.File(
        Path.Combine(logPath, "kafo-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true));

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 110 * 1024 * 1024;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(20);
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 110 * 1024 * 1024;
    options.ValueLengthLimit = 64 * 1024;
    options.MultipartHeadersLengthLimit = 16 * 1024;
});

builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.Configure<SmtpEmailOptions>(
    builder.Configuration.GetSection(SmtpEmailOptions.SectionName));
builder.Services.Configure<NotificationEmailOptions>(
    builder.Configuration.GetSection(NotificationEmailOptions.SectionName));
builder.Services.Configure<MalwareScanningOptions>(
    builder.Configuration.GetSection(MalwareScanningOptions.SectionName));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.RequireHeaderSymmetry = false;

    foreach (var value in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
    {
        if (IPAddress.TryParse(value, out var address))
            options.KnownProxies.Add(address);
    }
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("Kafo.Web");

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "Kafo.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.HeaderName = "RequestVerificationToken";
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.AddService<SecurityAuditActionFilter>();
});

builder.Services.AddMemoryCache(options => options.SizeLimit = 10_000);
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ILoginOtpService, LoginOtpService>();
builder.Services.AddScoped<IFileMalwareScanner, ClamAvFileMalwareScanner>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<ISecurityAuditService, SecurityAuditService>();
builder.Services.AddScoped<IPasswordSetupService, PasswordSetupService>();
builder.Services.AddScoped<LegacyPrivateFileMigrationService>();
builder.Services.AddHostedService<SecurityDataCleanupService>();
builder.Services.AddScoped<KafoCookieAuthenticationEvents>();
builder.Services.AddScoped<SecurityAuditActionFilter>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = KafoAuthSchemes.Admin;
        options.DefaultChallengeScheme = KafoAuthSchemes.Admin;
        options.DefaultSignInScheme = KafoAuthSchemes.Admin;
    })
    .AddCookie(KafoAuthSchemes.Admin, options => ConfigureCookie(
        options,
        builder,
        "/Admin/Login",
        "/Admin/Logout",
        "/Admin/Login",
        "Kafo.Admin.Auth"))
    .AddCookie(KafoAuthSchemes.Portal, options => ConfigureCookie(
        options,
        builder,
        "/Portal/Login",
        "/Portal/Logout",
        "/Portal/Login",
        "Kafo.Portal.Auth"))
    .AddCookie(KafoAuthSchemes.Donor, options => ConfigureCookie(
        options,
        builder,
        "/Portal/Login",
        "/Portal/Logout",
        "/Portal/Login",
        "Kafo.Donor.Auth"))
    .AddCookie(KafoAuthSchemes.Organization, options => ConfigureCookie(
        options,
        builder,
        "/Portal/Login",
        "/Portal/Logout",
        "/Portal/Login",
        "Kafo.Organization.Auth"));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
    options.EnableSensitiveDataLogging(false);
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("auth", context =>
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "/";
        var permitLimit = GetAuthenticationPermitLimit(path);

        return RateLimitPartition.GetSlidingWindowLimiter(
            $"auth:{GetClientKey(context)}:{path}",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(10),
                SegmentsPerWindow = 10,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.AddPolicy("public-forms", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"forms:{GetClientKey(context)}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        var response = httpContext.Response;

        var retryAfter = context.Lease.TryGetMetadata(
            MetadataName.RetryAfter,
            out var retryAfterMetadata)
                ? retryAfterMetadata
                : TimeSpan.FromMinutes(1);

        var retryAfterSeconds = Math.Clamp(
            (int)Math.Ceiling(retryAfter.TotalSeconds),
            1,
            900);

        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.Headers["Retry-After"] =
            retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        response.Headers["Cache-Control"] =
            "no-store, no-cache, must-revalidate";
        response.Headers["Pragma"] = "no-cache";
        response.Headers["Expires"] = "0";

        if (ExpectsHtmlResponse(httpContext.Request))
        {
            var returnUrl = httpContext.Request.Path.StartsWithSegments(
                "/Admin",
                StringComparison.OrdinalIgnoreCase)
                    ? "/Admin/Login"
                    : "/Portal/Login";

            var rateLimitUrl =
                "/RateLimit?retryAfterSeconds=" +
                retryAfterSeconds.ToString(CultureInfo.InvariantCulture) +
                "&returnUrl=" +
                Uri.EscapeDataString(returnUrl);

            response.StatusCode = StatusCodes.Status303SeeOther;
            response.Headers["Location"] = rateLimitUrl;
            return;
        }

        response.ContentType = "application/problem+json; charset=utf-8";

        var problem = new
        {
            type = "https://httpstatuses.com/429",
            title = "عدد محاولات كبير",
            status = StatusCodes.Status429TooManyRequests,
            detail = "يرجى الانتظار قليلًا قبل إعادة المحاولة.",
            retryAfterSeconds
        };

        await response.WriteAsync(
            JsonSerializer.Serialize(problem),
            cancellationToken);
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(scope.ServiceProvider);
    var legacyFiles = scope.ServiceProvider.GetRequiredService<LegacyPrivateFileMigrationService>();
    await legacyFiles.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<LegacyPrivateFileBlockMiddleware>();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        if (context.Context.Request.Path.StartsWithSegments("/uploads"))
            context.Context.Response.Headers.CacheControl = "public,max-age=86400";
    }
});

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<PortalAreaAccessMiddleware>();
app.UseMiddleware<AdminAccessMiddleware>();
app.UseMiddleware<PasswordChangeRequiredMiddleware>();

app.MapGet("/RateLimit", async context =>
{
    var retryAfterSeconds = ParseRetryAfterSeconds(
        context.Request.Query["retryAfterSeconds"].ToString());

    var requestedReturnUrl =
        context.Request.Query["returnUrl"].ToString();

    var returnUrl = IsSafeLocalPath(requestedReturnUrl)
        ? requestedReturnUrl
        : "/Portal/Login";

    context.Response.StatusCode =
        StatusCodes.Status429TooManyRequests;
    context.Response.ContentType =
        "text/html; charset=utf-8";
    context.Response.Headers["Retry-After"] =
        retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
    context.Response.Headers["Cache-Control"] =
        "no-store, no-cache, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";

    await context.Response.WriteAsync(
        BuildRateLimitPage(retryAfterSeconds, returnUrl),
        context.RequestAborted);
})
.DisableRateLimiting();

app.MapControllers();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();

static void ConfigureCookie(
    CookieAuthenticationOptions options,
    WebApplicationBuilder builder,
    string loginPath,
    string logoutPath,
    string accessDeniedPath,
    string cookieName)
{
    options.LoginPath = loginPath;
    options.LogoutPath = logoutPath;
    options.AccessDeniedPath = accessDeniedPath;
    options.Cookie.Name = cookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.SlidingExpiration = false;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.EventsType = typeof(KafoCookieAuthenticationEvents);
}

static string GetClientKey(HttpContext context)
    => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

static int GetAuthenticationPermitLimit(string path)
{
    if (path.Contains("resendotp", StringComparison.Ordinal))
        return 5;

    if (path.Contains("verifyotp", StringComparison.Ordinal))
        return 10;

    if (path.Contains("setpassword", StringComparison.Ordinal))
        return 10;

    // Password submission endpoints: account-level progressive lockout remains the main control.
    return 20;
}


static string ResolveConfiguredPath(string contentRoot, string? configuredPath, string fallbackPath)
{
    var value = string.IsNullOrWhiteSpace(configuredPath) ? fallbackPath : configuredPath.Trim();
    return Path.GetFullPath(Path.IsPathRooted(value) ? value : Path.Combine(contentRoot, value));
}

static bool ExpectsHtmlResponse(HttpRequest request)
{
    var accept = request.Headers.Accept.ToString();

    return accept.Contains(
        "text/html",
        StringComparison.OrdinalIgnoreCase);
}

static int ParseRetryAfterSeconds(string? value)
{
    if (!int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var seconds))
    {
        return 60;
    }

    return Math.Clamp(seconds, 1, 900);
}

static bool IsSafeLocalPath(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return false;

    var path = value.Trim();

    if (!path.StartsWith("/", StringComparison.Ordinal) ||
        path.StartsWith("//", StringComparison.Ordinal) ||
        path.StartsWith("/\\", StringComparison.Ordinal))
    {
        return false;
    }

    return path.IndexOfAny(['\r', '\n', '\0']) < 0;
}

static string BuildRateLimitPage(
    int retryAfterSeconds,
    string returnUrl)
{
    var encodedReturnUrl =
        HtmlEncoder.Default.Encode(returnUrl);

    var encodedSeconds =
        retryAfterSeconds.ToString(
            CultureInfo.InvariantCulture);

    return $$"""
        <!doctype html>
        <html lang="ar" dir="rtl">
        <head>
            <meta charset="utf-8">
            <meta name="viewport"
                  content="width=device-width, initial-scale=1">

            <meta name="robots"
                  content="noindex,nofollow">

            <title>يرجى الانتظار | جمعية كفو</title>

            <link rel="stylesheet"
                  href="/css/rate-limit.css">
        </head>

        <body>
            <main class="rate-limit-card"
                  id="rateLimitPage"
                  data-seconds="{{encodedSeconds}}"
                  data-return-url="{{encodedReturnUrl}}">

                <header class="rate-limit-head">
                    <div class="rate-limit-icon"
                         aria-hidden="true">⏱</div>

                    <h1>يرجى الانتظار قليلًا</h1>
                </header>

                <section class="rate-limit-body">
                    <p class="rate-limit-description">
                        تم تسجيل عدد من المحاولات خلال وقت قصير.
                        أوقفنا المحاولة مؤقتًا لحماية حسابك والمنصة.
                    </p>

                    <div class="rate-limit-timer"
                         aria-live="polite">
                        <span class="timer-label">
                            يمكنك إعادة المحاولة بعد
                        </span>

                        <strong id="rateLimitCountdown">
                            00:00
                        </strong>
                    </div>

                    <a class="rate-limit-button"
                       href="{{encodedReturnUrl}}">
                        العودة إلى تسجيل الدخول
                    </a>

                    <p class="rate-limit-note">
                        سيتم نقلك تلقائيًا بعد انتهاء الوقت.
                    </p>
                </section>
            </main>

            <script src="/js/rate-limit-page.js"
                    defer></script>
        </body>
        </html>
        """;
}

