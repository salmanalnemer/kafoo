using System.Security.Cryptography;

namespace Kafo.Web.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var nonce = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(18));

        context.Items["CspNonce"] = nonce;

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "SAMEORIGIN";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] =
                "camera=(), microphone=(), geolocation=(), payment=(), usb=()";

            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";

            var contentSecurityPolicy =
                "default-src 'self'; " +
                "base-uri 'self'; " +
                "object-src 'none'; " +
                "frame-ancestors 'self'; " +
                "form-action 'self'; " +
                $"script-src 'self' 'nonce-{nonce}' https://cdn.jsdelivr.net https://platform.twitter.com; " +
                "script-src-attr 'none' https://platform.twitter.com; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
                "font-src 'self' data: https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
                "img-src 'self' data: blob: https:; " +
                "media-src 'self' blob: https://video.twimg.com; " +
                "connect-src 'self' https://platform.twitter.com https://syndication.twitter.com https://api.twitter.com; " +
                "frame-src 'self' blob: https://platform.twitter.com https://syndication.twitter.com; " +
                "manifest-src 'self'; " +
                "worker-src 'self' blob:;";

            // لا نجبر المتصفح على تحويل HTTP إلى HTTPS
            // إلا عندما يكون الطلب نفسه يعمل عبر HTTPS.
            if (context.Request.IsHttps)
            {
                contentSecurityPolicy += " upgrade-insecure-requests;";
            }

            headers["Content-Security-Policy"] = contentSecurityPolicy;

            return Task.CompletedTask;
        });

        await _next(context);
    }
}