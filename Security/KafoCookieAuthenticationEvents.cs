using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Models.Donors;
using Kafo.Web.Models.Organizations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Security;

public sealed class KafoCookieAuthenticationEvents : CookieAuthenticationEvents
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<KafoCookieAuthenticationEvents> _logger;

    public KafoCookieAuthenticationEvents(
        ApplicationDbContext db,
        ILogger<KafoCookieAuthenticationEvents> logger)
    {
        _db = db;
        _logger = logger;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (principal == null)
        {
            await RejectAsync(context);
            return;
        }

        var portalType = principal.FindFirstValue("KafoPortalType");
        var claimedStamp = principal.FindFirstValue("KafoSecurityStamp");
        if (string.IsNullOrWhiteSpace(portalType) || string.IsNullOrWhiteSpace(claimedStamp))
        {
            await RejectAsync(context);
            return;
        }

        bool valid;
        bool mustChange;

        if (string.Equals(portalType, "Admin", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(principal.FindFirstValue("KafoAdminUserId"), out var adminId))
        {
            var state = await _db.AdminUsers.AsNoTracking()
                .Where(x => x.Id == adminId)
                .Select(x => new { x.IsActive, x.SecurityStamp, x.MustChangePassword })
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);
            valid = state is { IsActive: true } && FixedStampEquals(state.SecurityStamp, claimedStamp);
            mustChange = state?.MustChangePassword == true;
        }
        else if (string.Equals(portalType, "Donor", StringComparison.OrdinalIgnoreCase) &&
                 int.TryParse(principal.FindFirstValue("KafoDonorUserId"), out var donorId))
        {
            var state = await _db.DonorAccounts.AsNoTracking()
                .Where(x => x.Id == donorId)
                .Select(x => new { x.IsActive, x.SecurityStamp, x.MustChangePassword })
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);
            valid = state is { IsActive: true } && FixedStampEquals(state.SecurityStamp, claimedStamp);
            mustChange = state?.MustChangePassword == true;
        }
        else if (string.Equals(portalType, "Organization", StringComparison.OrdinalIgnoreCase) &&
                 int.TryParse(principal.FindFirstValue("KafoOrganizationUserId"), out var organizationId))
        {
            var state = await _db.OrganizationAccounts.AsNoTracking()
                .Where(x => x.Id == organizationId)
                .Select(x => new { x.IsActive, x.SecurityStamp, x.MustChangePassword })
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);
            valid = state is { IsActive: true } && FixedStampEquals(state.SecurityStamp, claimedStamp);
            mustChange = state?.MustChangePassword == true;
        }
        else
        {
            valid = false;
            mustChange = false;
        }

        if (!valid)
        {
            _logger.LogWarning("Rejected stale or invalid authentication cookie for portal type {PortalType}", portalType);
            await RejectAsync(context);
            return;
        }

        var currentClaim = principal.FindFirst("KafoMustChangePassword");
        var desired = mustChange ? "true" : "false";
        if (currentClaim?.Value != desired && principal.Identity is ClaimsIdentity identity)
        {
            if (currentClaim != null) identity.RemoveClaim(currentClaim);
            identity.AddClaim(new Claim("KafoMustChangePassword", desired));
            context.ReplacePrincipal(principal);
            context.ShouldRenew = true;
        }
    }

    private static bool FixedStampEquals(string? left, string? right)
        => !string.IsNullOrWhiteSpace(left) &&
           !string.IsNullOrWhiteSpace(right) &&
           string.Equals(left, right, StringComparison.Ordinal);

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(context.Scheme.Name);
    }
}
