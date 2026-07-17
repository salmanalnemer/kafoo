using System.Security.Claims;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kafo.Web.Security;

public sealed class SecurityAuditActionFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> AuditedMethods =
        new(["POST", "PUT", "PATCH", "DELETE"], StringComparer.OrdinalIgnoreCase);

    private readonly ISecurityAuditService _audit;

    public SecurityAuditActionFilter(ISecurityAuditService audit) => _audit = audit;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var executed = await next();
        var request = context.HttpContext.Request;

        if (!AuditedMethods.Contains(request.Method))
            return;

        var principal = context.HttpContext.User;
        var actorType = principal.FindFirstValue("KafoPortalType") ??
                        (principal.HasClaim(x => x.Type == "KafoAdminUserId") ? "Admin" : "Anonymous");
        var actorId = principal.FindFirstValue("KafoAdminUserId") ??
                      principal.FindFirstValue("KafoDonorUserId") ??
                      principal.FindFirstValue("KafoOrganizationUserId") ??
                      principal.FindFirstValue(ClaimTypes.NameIdentifier);

        var controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
        var action = context.RouteData.Values["action"]?.ToString() ?? "Unknown";
        var statusCode = context.HttpContext.Response.StatusCode;
        var success = executed.Exception is null && statusCode < 400;

        await _audit.WriteAsync(
            context.HttpContext,
            "StateChangingRequest",
            $"{request.Method} {controller}/{action} completed with HTTP {statusCode}.",
            success,
            success ? "Information" : "Warning",
            actorType,
            actorId,
            context.HttpContext.RequestAborted);
    }
}
