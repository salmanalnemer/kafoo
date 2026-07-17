using Kafo.Web.Security;
using Microsoft.AspNetCore.Authentication;

namespace Kafo.Web.Middleware;

public class PortalAreaAccessMiddleware
{
    private readonly RequestDelegate _next;

    public PortalAreaAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/Donor", StringComparison.OrdinalIgnoreCase))
        {
            RedirectLegacyPortalPath(context, "/Donor", "/Portal/Donor");
            return;
        }

        if (path.StartsWith("/Organizations", StringComparison.OrdinalIgnoreCase))
        {
            RedirectLegacyPortalPath(context, "/Organizations", "/Portal/Organization");
            return;
        }

        if (!path.StartsWith("/Portal", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (path.StartsWith("/Portal/Login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Portal/VerifyOtp", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Portal/ResendOtp", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Portal/Logout", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var authResult = await context.AuthenticateAsync(KafoAuthSchemes.Portal);

        if (!authResult.Succeeded || authResult.Principal == null)
        {
            var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
            context.Response.Redirect($"/Portal/Login?returnUrl={returnUrl}");
            return;
        }

        context.User = authResult.Principal;

        var portalType = context.User.FindFirst("KafoPortalType")?.Value;

        if (path.Equals("/Portal", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/Portal/", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect(GetHomePath(portalType));
            return;
        }

        if (path.StartsWith("/Portal/Donor", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(portalType, "Donor", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect(GetHomePath(portalType));
            return;
        }

        if (path.StartsWith("/Portal/Organization", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(portalType, "Organization", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect(GetHomePath(portalType));
            return;
        }

        await _next(context);
    }

    private static string GetHomePath(string? portalType)
    {
        return string.Equals(portalType, "Donor", StringComparison.OrdinalIgnoreCase)
            ? "/Portal/Donor/Dashboard"
            : string.Equals(portalType, "Organization", StringComparison.OrdinalIgnoreCase)
                ? "/Portal/Organization/Dashboard"
                : "/Portal/Login";
    }

    private static void RedirectLegacyPortalPath(HttpContext context, string oldPrefix, string newPrefix)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var query = context.Request.QueryString.Value ?? string.Empty;

        if (path.EndsWith("/Login", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/Portal/Login");
            return;
        }

        if (path.EndsWith("/Logout", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/Portal/Logout");
            return;
        }

        var suffix = path.Length > oldPrefix.Length ? path[oldPrefix.Length..] : string.Empty;
        var target = newPrefix + suffix + query;
        context.Response.Redirect(target);
    }
}
