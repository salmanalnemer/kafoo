using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Middleware;

public class AdminAccessMiddleware
{
    private readonly RequestDelegate _next;

    public AdminAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (!path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (IsPublicAdminPath(path))
        {
            await _next(context);
            return;
        }

        var authResult = await context.AuthenticateAsync(KafoAuthSchemes.Admin);
        if (!authResult.Succeeded || authResult.Principal == null)
        {
            var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
            context.Response.Redirect($"/Admin/Login?returnUrl={returnUrl}");
            return;
        }

        context.User = authResult.Principal;

        var userIdValue = context.User.FindFirstValue("KafoAdminUserId");
        if (!int.TryParse(userIdValue, out var adminUserId))
        {
            await context.SignOutAsync(KafoAuthSchemes.Admin);
            context.Response.Redirect("/Admin/Login");
            return;
        }

        var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        var adminUser = await db.AdminUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == adminUserId && x.IsActive);

        if (adminUser == null)
        {
            await context.SignOutAsync(KafoAuthSchemes.Admin);
            context.Response.Redirect("/Admin/Login");
            return;
        }

        var roleCode = await AdminRolePolicy.ResolveRoleAsync(db, adminUser);

        if (AdminRolePolicy.HasFullPageAccess(roleCode) || IsAlwaysAllowedAuthenticatedPath(path))
        {
            await _next(context);
            return;
        }

        var matchedPage = AdminPagesCatalog.Match(path);
        if (matchedPage == null)
        {
            await WriteAccessDeniedAsync(context, path);
            return;
        }

        var canAccess = await db.AdminPagePermissions
            .AsNoTracking()
            .AnyAsync(x =>
                x.AdminUserId == adminUserId &&
                x.PagePath == matchedPage.PagePath &&
                x.CanAccess);

        if (!canAccess)
        {
            await WriteAccessDeniedAsync(context, path);
            return;
        }

        await _next(context);
    }

    private static bool IsPublicAdminPath(string path)
    {
        return path.StartsWith("/Admin/Login", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/Admin/VerifyOtp", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/Admin/ResendOtp", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/Admin/Logout", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/Admin/assets", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAlwaysAllowedAuthenticatedPath(string path)
    {
        var normalized = AdminPagesCatalog.Normalize(path);

        return normalized.StartsWith("/Admin/Profile", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("/Admin/AccessDenied", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteAccessDeniedAsync(HttpContext context, string requestedPath)
    {
        if (context.Request.Headers.Accept.Any(x => x?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true) ||
            requestedPath.Contains("/Api/", StringComparison.OrdinalIgnoreCase) ||
            requestedPath.Contains("NotificationsApi", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "forbidden",
                message = "لا تملك صلاحية الوصول إلى هذه الصفحة."
            });
            return;
        }

        var encodedPath = Uri.EscapeDataString(requestedPath);
        context.Response.Redirect($"/Admin/AccessDenied?path={encodedPath}");
    }
}
