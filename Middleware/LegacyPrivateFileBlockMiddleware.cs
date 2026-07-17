namespace Kafo.Web.Middleware;

public sealed class LegacyPrivateFileBlockMiddleware
{
    private static readonly string[] BlockedPrefixes =
    [
        "/uploads/contact-attachments",
        "/uploads/contact-messages",
        "/uploads/feedback-attachments",
        "/uploads/job-applications-cv",
        "/uploads/job-applications-attachments",
        "/uploads/organization-candidate-cv",
        "/uploads/donor-reports"
    ];

    private readonly RequestDelegate _next;

    public LegacyPrivateFileBlockMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (BlockedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.Headers.CacheControl = "no-store";
            return;
        }

        await _next(context);
    }
}
