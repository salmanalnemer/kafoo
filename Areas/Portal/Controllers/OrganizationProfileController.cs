using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Security;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Portal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
public class OrganizationProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public OrganizationProfileController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    [HttpGet("/Portal/Organization/Profile")]
    public async Task<IActionResult> Index()
    {
        var organization = await _context.OrganizationAccounts.FindAsync(GetOrganizationId());
        return organization == null
            ? NotFound()
            : View("~/Areas/Portal/Views/OrganizationProfile/Index.cshtml", organization);
    }

    [HttpPost("/Portal/Organization/Profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        string name,
        string? activity,
        string? city,
        string? contactName,
        string? email,
        string? phone,
        IFormFile? logo)
    {
        var organizationId = GetOrganizationId();
        var item = await _context.OrganizationAccounts.FindAsync(organizationId);
        if (item == null)
            return NotFound();

        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(name))
            ModelState.AddModelError("name", "اسم الجهة مطلوب.");

        if (!PortalEmailPolicy.IsDeliverable(normalizedEmail))
            ModelState.AddModelError("email", "أدخل بريدًا إلكترونيًا حقيقيًا يمكنه استقبال رمز OTP.");
        else if (await _context.OrganizationAccounts.AnyAsync(x =>
                     x.Id != organizationId &&
                     x.Email != null &&
                     x.Email.ToLower() == normalizedEmail) ||
                 await _context.DonorAccounts.AnyAsync(x =>
                     x.Email != null &&
                     x.Email.ToLower() == normalizedEmail))
            ModelState.AddModelError("email", "البريد الإلكتروني مستخدم في حساب داعم أو جهة أخرى.");

        if (!ModelState.IsValid)
            return View("~/Areas/Portal/Views/OrganizationProfile/Index.cshtml", item);

        var emailChanged = !string.Equals(item.Email?.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase);
        var oldLogoPath = item.LogoPath;
        string? newLogoPath = null;
        if (logo is { Length: > 0 })
            newLogoPath = await _files.UploadAsync(logo, "organizations");

        item.Name = name.Trim();
        item.Activity = activity?.Trim();
        item.City = city?.Trim();
        item.ContactName = contactName?.Trim();
        item.Email = normalizedEmail;
        item.Phone = phone?.Trim();
        if (newLogoPath != null)
            item.LogoPath = newLogoPath;
        if (emailChanged)
            item.SecurityStamp = LoginSecurity.NewSecurityStamp();
        item.UpdatedAt = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            if (newLogoPath != null)
                _files.Delete(newLogoPath);
            throw;
        }

        if (newLogoPath != null && !string.IsNullOrWhiteSpace(oldLogoPath))
            _files.Delete(oldLogoPath);

        if (emailChanged)
        {
            await HttpContext.SignOutAsync(KafoAuthSchemes.Portal);
            TempData["LoginError"] = "تم تحديث البريد الإلكتروني. سجل الدخول مجددًا وسيتم إرسال رمز التحقق إلى البريد الجديد.";
            return Redirect("/Portal/Login");
        }

        TempData["Success"] = "تم تحديث بيانات الجهة.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/Portal/Organization/Profile/Password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(PortalChangePasswordViewModel model)
    {
        var organizationId = GetOrganizationId();
        var organization = await _context.OrganizationAccounts
            .FirstOrDefaultAsync(x => x.Id == organizationId);

        if (organization == null)
            return NotFound();

        foreach (var error in PasswordPolicy.Validate(model.NewPassword))
            ModelState.AddModelError(nameof(model.NewPassword), error);

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage));
            return Redirect("/Portal/Organization/Profile");
        }

        if (!AdminPasswordHasher.VerifyPassword(
                model.CurrentPassword,
                organization.PasswordHash,
                organization.PasswordSalt))
        {
            TempData["Error"] = "كلمة المرور الحالية غير صحيحة.";
            return Redirect("/Portal/Organization/Profile");
        }

        if (AdminPasswordHasher.VerifyPassword(
                model.NewPassword,
                organization.PasswordHash,
                organization.PasswordSalt))
        {
            TempData["Error"] = "كلمة المرور الجديدة يجب أن تختلف عن كلمة المرور الحالية.";
            return Redirect("/Portal/Organization/Profile");
        }

        var hashed = AdminPasswordHasher.HashPassword(model.NewPassword);
        organization.PasswordHash = hashed.Hash;
        organization.PasswordSalt = hashed.Salt;
        organization.SecurityStamp = LoginSecurity.NewSecurityStamp();
        organization.MustChangePassword = false;
        organization.AccessFailedCount = 0;
        organization.LockoutEndUtc = null;
        organization.PasswordChangedAtUtc = DateTime.UtcNow;
        organization.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        await HttpContext.SignOutAsync(KafoAuthSchemes.Portal);

        TempData["LoginError"] = "تم تغيير كلمة المرور. سجل الدخول مرة أخرى لحماية حسابك.";
        return Redirect("/Portal/Login");
    }

    private int GetOrganizationId()
        => int.TryParse(User.FindFirstValue("KafoOrganizationUserId"), out var id) ? id : 0;
}
