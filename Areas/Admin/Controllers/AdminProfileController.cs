using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Security;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AdminProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public AdminProfileController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    [HttpGet("/Admin/Profile")]
    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentAdminUserAsync();

        if (user == null)
            return Redirect("/Admin/Login");

        var profile = await BuildProfileModelAsync(user);

        ViewBag.PasswordModel = new AdminChangePasswordViewModel();

        return View("~/Areas/Admin/Views/AdminProfile/Index.cshtml", profile);
    }

    [HttpPost("/Admin/Profile/UpdateInfo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateInfo(AdminProfileViewModel model, IFormFile? profileImage)
    {
        var user = await GetCurrentAdminUserAsync();

        if (user == null)
            return Redirect("/Admin/Login");

        ModelState.Remove(nameof(model.ProfileImagePath));
        ModelState.Remove(nameof(model.IsSuperAdmin));
        ModelState.Remove(nameof(model.RoleCode));
        ModelState.Remove(nameof(model.RoleLabel));
        ModelState.Remove(nameof(model.LastLoginAt));
        ModelState.Remove(nameof(model.CreatedAt));

        var normalizedEmail = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedUserName = model.UserName.Trim();
        var duplicate = await _context.AdminUsers
            .AnyAsync(x =>
                x.Id != user.Id &&
                (
                    x.UserName == normalizedUserName ||
                    (x.Email != null && x.Email.ToLower() == normalizedEmail)
                ));

        if (duplicate)
            ModelState.AddModelError("", "اسم المستخدم أو البريد مستخدم مسبقاً.");

        if (!ModelState.IsValid)
        {
            var roleCode = await AdminRolePolicy.ResolveRoleAsync(_context, user);
            model.ProfileImagePath = user.ProfileImagePath;
            model.IsSuperAdmin = user.IsSuperAdmin;
            model.RoleCode = roleCode;
            model.RoleLabel = AdminRolePolicy.GetLabel(roleCode);
            model.LastLoginAt = user.LastLoginAt;
            model.CreatedAt = user.CreatedAt;
            ViewBag.PasswordModel = new AdminChangePasswordViewModel();
            return View("~/Areas/Admin/Views/AdminProfile/Index.cshtml", model);
        }

        var emailChanged = !string.Equals(user.Email?.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase);
        var oldProfileImage = user.ProfileImagePath;
        string? newProfileImage = null;

        if (profileImage is { Length: > 0 })
            newProfileImage = await _files.UploadAsync(profileImage, "admin-profiles");

        user.FullName = model.FullName.Trim();
        user.UserName = normalizedUserName;
        user.Email = normalizedEmail;
        user.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        if (newProfileImage != null)
            user.ProfileImagePath = newProfileImage;
        if (emailChanged)
            user.SecurityStamp = LoginSecurity.NewSecurityStamp();
        user.UpdatedAt = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            if (newProfileImage != null)
                _files.Delete(newProfileImage);
            throw;
        }

        if (newProfileImage != null && !string.IsNullOrWhiteSpace(oldProfileImage))
            _files.Delete(oldProfileImage);

        if (emailChanged)
        {
            await HttpContext.SignOutAsync(KafoAuthSchemes.Admin);
            TempData["LoginError"] = "تم تحديث البريد الإلكتروني. سجل الدخول مجددًا وسيتم إرسال رمز التحقق إلى البريد الجديد.";
            return Redirect("/Admin/Login");
        }

        await RefreshSignInAsync(user.Id);
        TempData["Success"] = "تم تحديث بيانات الحساب بنجاح.";
        return Redirect("/Admin/Profile");
    }

    [HttpPost("/Admin/Profile/ChangePassword")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(AdminChangePasswordViewModel passwordModel)
    {
        var user = await GetCurrentAdminUserAsync();

        if (user == null)
            return Redirect("/Admin/Login");

        foreach (var error in PasswordPolicy.Validate(passwordModel.NewPassword))
            ModelState.AddModelError(nameof(passwordModel.NewPassword), error);

        if (!ModelState.IsValid)
        {
            var profile = await BuildProfileModelAsync(user);
            ViewBag.PasswordModel = passwordModel;
            return View("~/Areas/Admin/Views/AdminProfile/Index.cshtml", profile);
        }

        var validCurrentPassword = AdminPasswordHasher.VerifyPassword(
            passwordModel.CurrentPassword,
            user.PasswordHash,
            user.PasswordSalt);

        if (!validCurrentPassword)
        {
            ModelState.AddModelError(nameof(passwordModel.CurrentPassword), "كلمة المرور الحالية غير صحيحة.");

            var profile = await BuildProfileModelAsync(user);
            ViewBag.PasswordModel = passwordModel;
            return View("~/Areas/Admin/Views/AdminProfile/Index.cshtml", profile);
        }

        if (AdminPasswordHasher.VerifyPassword(passwordModel.NewPassword, user.PasswordHash, user.PasswordSalt))
        {
            ModelState.AddModelError(nameof(passwordModel.NewPassword), "كلمة المرور الجديدة يجب أن تختلف عن الحالية.");
            var profile = await BuildProfileModelAsync(user);
            ViewBag.PasswordModel = passwordModel;
            return View("~/Areas/Admin/Views/AdminProfile/Index.cshtml", profile);
        }

        var hashed = AdminPasswordHasher.HashPassword(passwordModel.NewPassword);
        user.PasswordHash = hashed.Hash;
        user.PasswordSalt = hashed.Salt;
        user.SecurityStamp = LoginSecurity.NewSecurityStamp();
        user.MustChangePassword = false;
        user.AccessFailedCount = 0;
        user.LockoutEndUtc = null;
        user.PasswordChangedAtUtc = DateTime.UtcNow;
        user.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        await HttpContext.SignOutAsync(KafoAuthSchemes.Admin);

        TempData["LoginError"] = "تم تغيير كلمة المرور. سجل الدخول مرة أخرى لحماية حسابك.";
        return Redirect("/Admin/Login");
    }

    private async Task<Kafo.Web.Models.AdminUser?> GetCurrentAdminUserAsync()
    {
        var userIdValue = User.Claims.FirstOrDefault(x => x.Type == "KafoAdminUserId")?.Value;

        if (!int.TryParse(userIdValue, out var userId))
            return null;

        return await _context.AdminUsers.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
    }

    private async Task<AdminProfileViewModel> BuildProfileModelAsync(Kafo.Web.Models.AdminUser user)
    {
        var roleCode = await AdminRolePolicy.ResolveRoleAsync(_context, user);

        return new AdminProfileViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName,
            Email = user.Email ?? string.Empty,
            Phone = user.Phone,
            ProfileImagePath = user.ProfileImagePath,
            IsSuperAdmin = user.IsSuperAdmin,
            RoleCode = roleCode,
            RoleLabel = AdminRolePolicy.GetLabel(roleCode),
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }

    private async Task RefreshSignInAsync(int userId)
    {
        var user = await _context.AdminUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            return;

        var roleCode = await AdminRolePolicy.ResolveRoleAsync(_context, user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new("KafoPortalType", "Admin"),
            new("KafoAdminUserId", user.Id.ToString()),
            new("KafoIsSuperAdmin", user.IsSuperAdmin ? "true" : "false"),
            new("KafoAdminRole", roleCode),
            new("KafoSecurityStamp", user.SecurityStamp),
            new("KafoMustChangePassword", user.MustChangePassword ? "true" : "false"),
            new(ClaimTypes.Role, roleCode)
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        var identity = new ClaimsIdentity(claims, KafoAuthSchemes.Admin);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            KafoAuthSchemes.Admin,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2),
                AllowRefresh = false
            });
    }
}
