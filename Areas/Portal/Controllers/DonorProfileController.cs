using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Security;
using Kafo.Web.ViewModels.Donor;
using Kafo.Web.ViewModels.Portal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
public class DonorProfileController : Controller
{
    private readonly ApplicationDbContext _context;

    public DonorProfileController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Portal/Donor/Profile")]
    public async Task<IActionResult> Index()
    {
        var donorId = GetDonorId();
        var donor = await _context.DonorAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == donorId);
        if (donor == null) return NotFound();

        var model = new DonorProfileViewModel
        {
            Id = donor.Id,
            FullName = donor.FullName,
            DonorType = donor.DonorType,
            OrganizationName = donor.OrganizationName,
            Email = donor.Email ?? string.Empty,
            Phone = donor.Phone,
            LastLoginAt = donor.LastLoginAt,
            CreatedAt = donor.CreatedAt
        };

        return View("~/Areas/Portal/Views/DonorProfile/Index.cshtml", model);
    }

    [HttpPost("/Portal/Donor/Profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(DonorProfileViewModel model)
    {
        var donorId = GetDonorId();
        var donor = await _context.DonorAccounts.FirstOrDefaultAsync(x => x.Id == donorId);
        if (donor == null) return NotFound();

        var normalizedEmail = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (!PortalEmailPolicy.IsDeliverable(normalizedEmail))
        {
            ModelState.AddModelError(nameof(model.Email), "أدخل بريدًا إلكترونيًا حقيقيًا يمكنه استقبال رمز OTP.");
        }
        else
        {
            var emailExists = await _context.DonorAccounts.AnyAsync(x =>
                    x.Id != donorId &&
                    x.Email != null &&
                    x.Email.ToLower() == normalizedEmail) ||
                await _context.OrganizationAccounts.AnyAsync(x =>
                    x.Email != null &&
                    x.Email.ToLower() == normalizedEmail);
            if (emailExists)
                ModelState.AddModelError(nameof(model.Email), "البريد الإلكتروني مستخدم في حساب داعم أو جهة أخرى.");
        }

        if (!ModelState.IsValid)
        {
            model.Id = donor.Id;
            model.LastLoginAt = donor.LastLoginAt;
            model.CreatedAt = donor.CreatedAt;
            return View("~/Areas/Portal/Views/DonorProfile/Index.cshtml", model);
        }

        var emailChanged = !string.Equals(donor.Email?.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase);
        donor.FullName = model.FullName.Trim();
        donor.DonorType = string.IsNullOrWhiteSpace(model.DonorType) ? "فرد" : model.DonorType.Trim();
        donor.OrganizationName = string.IsNullOrWhiteSpace(model.OrganizationName) ? null : model.OrganizationName.Trim();
        donor.Email = normalizedEmail;
        donor.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        if (emailChanged)
            donor.SecurityStamp = LoginSecurity.NewSecurityStamp();
        donor.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        if (emailChanged)
        {
            await HttpContext.SignOutAsync(KafoAuthSchemes.Portal);
            TempData["LoginError"] = "تم تحديث البريد الإلكتروني. سجل الدخول مجددًا وسيتم إرسال رمز التحقق إلى البريد الجديد.";
            return Redirect("/Portal/Login");
        }

        TempData["Success"] = "تم تحديث بيانات الحساب بنجاح.";
        return Redirect("/Portal/Donor/Profile");
    }

    [HttpPost("/Portal/Donor/Profile/Password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(PortalChangePasswordViewModel model)
    {
        var donorId = GetDonorId();
        var donor = await _context.DonorAccounts.FirstOrDefaultAsync(x => x.Id == donorId);

        if (donor == null)
            return NotFound();

        foreach (var error in PasswordPolicy.Validate(model.NewPassword))
            ModelState.AddModelError(nameof(model.NewPassword), error);

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage));
            return Redirect("/Portal/Donor/Profile");
        }

        if (!AdminPasswordHasher.VerifyPassword(
                model.CurrentPassword,
                donor.PasswordHash,
                donor.PasswordSalt))
        {
            TempData["Error"] = "كلمة المرور الحالية غير صحيحة.";
            return Redirect("/Portal/Donor/Profile");
        }

        if (AdminPasswordHasher.VerifyPassword(
                model.NewPassword,
                donor.PasswordHash,
                donor.PasswordSalt))
        {
            TempData["Error"] = "كلمة المرور الجديدة يجب أن تختلف عن كلمة المرور الحالية.";
            return Redirect("/Portal/Donor/Profile");
        }

        var hashed = AdminPasswordHasher.HashPassword(model.NewPassword);
        donor.PasswordHash = hashed.Hash;
        donor.PasswordSalt = hashed.Salt;
        donor.SecurityStamp = LoginSecurity.NewSecurityStamp();
        donor.MustChangePassword = false;
        donor.AccessFailedCount = 0;
        donor.LockoutEndUtc = null;
        donor.PasswordChangedAtUtc = DateTime.UtcNow;
        donor.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        await HttpContext.SignOutAsync(KafoAuthSchemes.Portal);

        TempData["LoginError"] = "تم تغيير كلمة المرور. سجل الدخول مرة أخرى لحماية حسابك.";
        return Redirect("/Portal/Login");
    }

    private int GetDonorId()
    {
        var value = User.FindFirstValue("KafoDonorUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var donorId) ? donorId : 0;
    }
}
