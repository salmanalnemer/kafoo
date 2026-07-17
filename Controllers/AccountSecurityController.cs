using Kafo.Web.Data;
using Kafo.Web.Security;
using Kafo.Web.Services.Implementations;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountSecurityController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISecurityAuditService _audit;

    public AccountSecurityController(ApplicationDbContext db, ISecurityAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet("/Account/SetPassword")]
    public async Task<IActionResult> SetPassword(string token, CancellationToken cancellationToken)
    {
        var state = await ResolveTokenAsync(token, cancellationToken);
        if (state == null)
            return View("~/Views/AccountSecurity/InvalidToken.cshtml");

        return View("~/Views/AccountSecurity/SetPassword.cshtml", new SetPasswordViewModel
        {
            Token = token,
            AccountLabel = state.Value.AccountLabel,
            DisplayName = state.Value.DisplayName
        });
    }

    [HttpPost("/Account/SetPassword")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPassword(
        SetPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        var state = await ResolveTokenAsync(model.Token, cancellationToken);
        if (state == null)
            return View("~/Views/AccountSecurity/InvalidToken.cshtml");

        foreach (var error in PasswordPolicy.Validate(model.NewPassword))
            ModelState.AddModelError(nameof(model.NewPassword), error);

        if (!ModelState.IsValid)
        {
            model.AccountLabel = state.Value.AccountLabel;
            model.DisplayName = state.Value.DisplayName;
            return View("~/Views/AccountSecurity/SetPassword.cshtml", model);
        }

        var hashed = AdminPasswordHasher.HashPassword(model.NewPassword);
        var now = DateTime.UtcNow;
        var setupToken = state.Value.Token;

        switch (setupToken.AccountType)
        {
            case "Admin":
            {
                var account = await _db.AdminUsers.FirstOrDefaultAsync(x => x.Id == setupToken.AccountId, cancellationToken);
                if (account == null) return View("~/Views/AccountSecurity/InvalidToken.cshtml");
                account.PasswordHash = hashed.Hash;
                account.PasswordSalt = hashed.Salt;
                account.SecurityStamp = LoginSecurity.NewSecurityStamp();
                account.MustChangePassword = false;
                account.AccessFailedCount = 0;
                account.LockoutEndUtc = null;
                account.PasswordChangedAtUtc = now;
                account.UpdatedAt = DateTime.Now;
                break;
            }
            case "Donor":
            {
                var account = await _db.DonorAccounts.FirstOrDefaultAsync(x => x.Id == setupToken.AccountId, cancellationToken);
                if (account == null) return View("~/Views/AccountSecurity/InvalidToken.cshtml");
                account.PasswordHash = hashed.Hash;
                account.PasswordSalt = hashed.Salt;
                account.SecurityStamp = LoginSecurity.NewSecurityStamp();
                account.MustChangePassword = false;
                account.AccessFailedCount = 0;
                account.LockoutEndUtc = null;
                account.PasswordChangedAtUtc = now;
                account.UpdatedAt = DateTime.Now;
                break;
            }
            case "Organization":
            {
                var account = await _db.OrganizationAccounts.FirstOrDefaultAsync(x => x.Id == setupToken.AccountId, cancellationToken);
                if (account == null) return View("~/Views/AccountSecurity/InvalidToken.cshtml");
                account.PasswordHash = hashed.Hash;
                account.PasswordSalt = hashed.Salt;
                account.SecurityStamp = LoginSecurity.NewSecurityStamp();
                account.MustChangePassword = false;
                account.AccessFailedCount = 0;
                account.LockoutEndUtc = null;
                account.PasswordChangedAtUtc = now;
                account.UpdatedAt = DateTime.Now;
                break;
            }
            default:
                return View("~/Views/AccountSecurity/InvalidToken.cshtml");
        }

        var activeTokens = await _db.PasswordSetupTokens
            .Where(x =>
                x.AccountType == setupToken.AccountType &&
                x.AccountId == setupToken.AccountId &&
                x.UsedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var item in activeTokens)
            item.UsedAtUtc = now;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(
            HttpContext,
            "PasswordSetupCompleted",
            "A one-time password setup link was used successfully.",
            success: true,
            actorType: setupToken.AccountType,
            actorId: setupToken.AccountId.ToString(),
            cancellationToken: cancellationToken);

        ViewBag.LoginUrl = setupToken.AccountType == "Admin" ? "/Admin/Login" : "/Portal/Login";
        return View("~/Views/AccountSecurity/PasswordSet.cshtml");
    }

    private async Task<(Kafo.Web.Models.PasswordSetupToken Token, string DisplayName, string AccountLabel)?> ResolveTokenAsync(
        string rawToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || rawToken.Length > 200)
            return null;

        var hash = PasswordSetupService.HashToken(rawToken);
        var token = await _db.PasswordSetupTokens
            .FirstOrDefaultAsync(x =>
                x.TokenHash == hash &&
                x.UsedAtUtc == null &&
                x.ExpiresAtUtc > DateTime.UtcNow,
                cancellationToken);
        if (token == null)
            return null;

        if (token.AccountType == "Admin")
        {
            var name = await _db.AdminUsers.AsNoTracking()
                .Where(x => x.Id == token.AccountId && x.IsActive)
                .Select(x => x.FullName)
                .FirstOrDefaultAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(name) ? null : (token, name, "لوحة تحكم الإدارة");
        }

        if (token.AccountType == "Donor")
        {
            var name = await _db.DonorAccounts.AsNoTracking()
                .Where(x => x.Id == token.AccountId && x.IsActive)
                .Select(x => x.FullName)
                .FirstOrDefaultAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(name) ? null : (token, name, "بوابة الداعمين");
        }

        if (token.AccountType == "Organization")
        {
            var name = await _db.OrganizationAccounts.AsNoTracking()
                .Where(x => x.Id == token.AccountId && x.IsActive)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(name) ? null : (token, name, "بوابة الجهات والشركات");
        }

        return null;
    }

}
