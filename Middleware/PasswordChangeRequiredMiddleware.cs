namespace Kafo.Web.Middleware;

public sealed class PasswordChangeRequiredMiddleware
{
    private readonly RequestDelegate _next;

    public PasswordChangeRequiredMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            string.Equals(context.User.FindFirst("KafoMustChangePassword")?.Value, "true", StringComparison.OrdinalIgnoreCase))
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var portalType = context.User.FindFirst("KafoPortalType")?.Value;

            if (!IsAllowedPath(path, portalType))
            {
                var target = string.Equals(portalType, "Admin", StringComparison.OrdinalIgnoreCase)
                    ? "/Admin/Profile#change-password"
                    : string.Equals(portalType, "Donor", StringComparison.OrdinalIgnoreCase)
                        ? "/Portal/Donor/Profile#change-password"
                        : "/Portal/Organization/Profile#change-password";
                context.Response.Redirect(target);
                return;
            }
        }

        await _next(context);
    }

    private static bool IsAllowedPath(string path, string? portalType)
    {
        if (path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(portalType, "Admin", StringComparison.OrdinalIgnoreCase)
            ? path.StartsWith("/Admin/Profile", StringComparison.OrdinalIgnoreCase) ||
              path.StartsWith("/Admin/Logout", StringComparison.OrdinalIgnoreCase)
            : path.StartsWith("/Portal/Donor/Profile", StringComparison.OrdinalIgnoreCase) ||
              path.StartsWith("/Portal/Organization/Profile", StringComparison.OrdinalIgnoreCase) ||
              path.StartsWith("/Portal/Logout", StringComparison.OrdinalIgnoreCase);
    }
}
