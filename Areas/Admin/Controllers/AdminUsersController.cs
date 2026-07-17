using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Security;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AdminUsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordSetupService _passwordSetup;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        ApplicationDbContext context,
        IPasswordSetupService passwordSetup,
        ILogger<AdminUsersController> logger)
    {
        _context = context;
        _passwordSetup = passwordSetup;
        _logger = logger;
    }

    [HttpGet("/Admin/Users")]
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        if (actor == null || !AdminRolePolicy.CanManageUsers(actor.RoleCode))
            return Forbid();

        var query = _context.AdminUsers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(x =>
                x.FullName.Contains(q) ||
                x.UserName.Contains(q) ||
                (x.Email != null && x.Email.Contains(q)) ||
                (x.Phone != null && x.Phone.Contains(q)));
        }

        var users = await query.ToListAsync(cancellationToken);
        var administrationManagerIds = await AdminRolePolicy
            .GetAdministrationManagerIdsAsync(_context, cancellationToken);

        var items = users
            .Select(user =>
            {
                var roleCode = ResolveRole(user, administrationManagerIds);
                var canManageTarget = AdminRolePolicy.CanManageTarget(actor.RoleCode, roleCode);

                return new AdminUserListItemViewModel
                {
                    User = user,
                    RoleCode = roleCode,
                    RoleLabel = AdminRolePolicy.GetLabel(roleCode),
                    CanEdit = canManageTarget,
                    CanDelete = canManageTarget && user.Id != actor.User.Id,
                    CanManagePermissions = canManageTarget && roleCode == AdminRolePolicy.Supervisor
                };
            })
            .OrderBy(x => RoleSortOrder(x.RoleCode))
            .ThenBy(x => x.User.FullName)
            .ToList();

        return View("~/Areas/Admin/Views/AdminUsers/Index.cshtml", new AdminUsersIndexViewModel
        {
            Search = q ?? string.Empty,
            CurrentUserRoleCode = actor.RoleCode,
            Users = items
        });
    }

    [HttpGet("/Admin/Users/Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        if (actor == null || !AdminRolePolicy.CanManageUsers(actor.RoleCode))
            return Forbid();

        ViewBag.ActorRoleCode = actor.RoleCode;

        return View("~/Areas/Admin/Views/AdminUsers/Create.cshtml", new AdminUserFormViewModel
        {
            RoleCode = AdminRolePolicy.Supervisor,
            IsActive = true
        });
    }

    [HttpPost("/Admin/Users/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AdminUserFormViewModel model,
        CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        if (actor == null || !AdminRolePolicy.CanManageUsers(actor.RoleCode))
            return Forbid();

        ViewBag.ActorRoleCode = actor.RoleCode;
        ModelState.Remove(nameof(model.Password));

        if (!AdminRolePolicy.CanAssignRole(actor.RoleCode, model.RoleCode))
            ModelState.AddModelError(nameof(model.RoleCode), "لا تملك صلاحية إنشاء هذا النوع من المستخدمين.");

        var normalizedEmail = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedUserName = (model.UserName ?? string.Empty).Trim();

        if (!PortalEmailPolicy.IsDeliverable(normalizedEmail))
            ModelState.AddModelError(nameof(model.Email), "استخدم بريدًا إلكترونيًا حقيقيًا لاستقبال كلمة المرور ورمز OTP.");

        var exists = await _context.AdminUsers.AnyAsync(x =>
                x.UserName == normalizedUserName ||
                (x.Email != null && x.Email.ToLower() == normalizedEmail),
            cancellationToken);

        if (exists)
            ModelState.AddModelError(string.Empty, "اسم المستخدم أو البريد مستخدم مسبقًا.");

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/AdminUsers/Create.cshtml", model);

        var initialSecret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
        var hashed = AdminPasswordHasher.HashPassword(initialSecret);

        var user = new AdminUser
        {
            FullName = model.FullName.Trim(),
            UserName = normalizedUserName,
            Email = normalizedEmail,
            Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
            PasswordHash = hashed.Hash,
            PasswordSalt = hashed.Salt,
            SecurityStamp = LoginSecurity.NewSecurityStamp(),
            MustChangePassword = true,
            IsSuperAdmin = model.RoleCode == AdminRolePolicy.SystemManager,
            IsActive = model.IsActive,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.AdminUsers.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        await AdminRolePolicy.ApplyRoleAsync(
            _context,
            user,
            model.RoleCode,
            clearPagePermissions: true,
            cancellationToken: cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _passwordSetup.IssueAsync(
                "Admin",
                user.Id,
                user.Email!,
                user.FullName,
                $"لوحة التحكم - {AdminRolePolicy.GetLabel(model.RoleCode)}",
                actor.User.Id,
                HttpContext,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to email credentials for new admin user {AdminUserId}", user.Id);

            var permissions = await _context.AdminPagePermissions
                .Where(x => x.AdminUserId == user.Id)
                .ToListAsync(CancellationToken.None);

            _context.AdminPagePermissions.RemoveRange(permissions);
            _context.AdminUsers.Remove(user);
            await _context.SaveChangesAsync(CancellationToken.None);

            ModelState.AddModelError(string.Empty,
                "تعذر إرسال رابط إعداد كلمة المرور، لذلك لم يتم إنشاء الحساب. تحقق من SMTP ثم حاول مجددًا.");

            return View("~/Areas/Admin/Views/AdminUsers/Create.cshtml", model);
        }

        TempData["Success"] = $"تم إنشاء المستخدم وإرسال رابط آمن لإعداد كلمة المرور إلى {normalizedEmail}.";

        return model.RoleCode == AdminRolePolicy.Supervisor
            ? Redirect($"/Admin/Users/Permissions/{user.Id}")
            : Redirect("/Admin/Users");
    }

    [HttpGet("/Admin/Users/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        if (actor == null || !AdminRolePolicy.CanManageUsers(actor.RoleCode))
            return Forbid();

        var user = await _context.AdminUsers.FindAsync(new object[] { id }, cancellationToken);
        if (user == null)
            return NotFound();

        var targetRole = await AdminRolePolicy.ResolveRoleAsync(_context, user, cancellationToken);
        if (!AdminRolePolicy.CanManageTarget(actor.RoleCode, targetRole))
            return Forbid();

        ViewBag.ActorRoleCode = actor.RoleCode;
        ViewBag.TargetRoleCode = targetRole;

        return View("~/Areas/Admin/Views/AdminUsers/Edit.cshtml", new AdminUserFormViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName,
            Email = user.Email ?? string.Empty,
            Phone = user.Phone,
            RoleCode = targetRole,
            IsActive = user.IsActive
        });
    }

    [HttpPost("/Admin/Users/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        AdminUserFormViewModel model,
        CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        if (actor == null || !AdminRolePolicy.CanManageUsers(actor.RoleCode))
            return Forbid();

        if (id != model.Id)
            return BadRequest();

        var user = await _context.AdminUsers.FindAsync(new object[] { id }, cancellationToken);
        if (user == null)
            return NotFound();

        var oldRole = await AdminRolePolicy.ResolveRoleAsync(_context, user, cancellationToken);
        if (!AdminRolePolicy.CanManageTarget(actor.RoleCode, oldRole))
            return Forbid();

        ViewBag.ActorRoleCode = actor.RoleCode;
        ViewBag.TargetRoleCode = oldRole;

        if (!AdminRolePolicy.CanAssignRole(actor.RoleCode, model.RoleCode))
            ModelState.AddModelError(nameof(model.RoleCode), "لا تملك صلاحية تعيين هذا النوع من المستخدمين.");

        if (user.Id == actor.User.Id &&
            (model.RoleCode != oldRole || !model.IsActive))
        {
            ModelState.AddModelError(string.Empty, "لا يمكنك تغيير دور حسابك الحالي أو تعطيله من هذه الصفحة.");
        }

        if (oldRole == AdminRolePolicy.SystemManager &&
            (model.RoleCode != AdminRolePolicy.SystemManager || !model.IsActive))
        {
            var otherActiveSystemManagers = await _context.AdminUsers
                .CountAsync(x => x.Id != user.Id && x.IsSuperAdmin && x.IsActive, cancellationToken);

            if (otherActiveSystemManagers == 0)
                ModelState.AddModelError(string.Empty, "يجب أن يبقى في النظام مدير نظام فعال واحد على الأقل.");
        }

        var normalizedEmail = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedUserName = (model.UserName ?? string.Empty).Trim();

        if (!PortalEmailPolicy.IsDeliverable(normalizedEmail))
            ModelState.AddModelError(nameof(model.Email), "استخدم بريدًا إلكترونيًا حقيقيًا لاستقبال رمز OTP والإشعارات.");

        var duplicate = await _context.AdminUsers.AnyAsync(x =>
                x.Id != id &&
                (x.UserName == normalizedUserName ||
                 (x.Email != null && x.Email.ToLower() == normalizedEmail)),
            cancellationToken);

        if (duplicate)
            ModelState.AddModelError(string.Empty, "اسم المستخدم أو البريد مستخدم مسبقًا.");

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            foreach (var error in PasswordPolicy.Validate(model.Password))
                ModelState.AddModelError(nameof(model.Password), error);
        }

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/AdminUsers/Edit.cshtml", model);

        user.FullName = model.FullName.Trim();
        user.UserName = normalizedUserName;
        user.Email = normalizedEmail;
        user.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        user.IsActive = model.IsActive;
        user.UpdatedAt = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var hashed = AdminPasswordHasher.HashPassword(model.Password);
            user.PasswordHash = hashed.Hash;
            user.PasswordSalt = hashed.Salt;
            user.SecurityStamp = LoginSecurity.NewSecurityStamp();
            user.MustChangePassword = false;
            user.AccessFailedCount = 0;
            user.LockoutEndUtc = null;
            user.PasswordChangedAtUtc = DateTime.UtcNow;
        }

        var clearPermissions = oldRole != model.RoleCode || model.RoleCode != AdminRolePolicy.Supervisor;
        await AdminRolePolicy.ApplyRoleAsync(
            _context,
            user,
            model.RoleCode,
            clearPagePermissions: clearPermissions,
            cancellationToken: cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "تم تحديث بيانات المستخدم بنجاح.";
        return Redirect("/Admin/Users");
    }

    [HttpGet("/Admin/Users/Permissions/{id:int}")]
    public async Task<IActionResult> Permissions(int id, CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        if (actor == null || !AdminRolePolicy.CanManageUsers(actor.RoleCode))
            return Forbid();

        var user = await _context.AdminUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user == null)
            return NotFound();

        var targetRole = await AdminRolePolicy.ResolveRoleAsync(_context, user, cancellationToken);
        if (!AdminRolePolicy.CanManageTarget(actor.RoleCode, targetRole) ||
            targetRole != AdminRolePolicy.Supervisor)
        {
            TempData["Error"] = "الصلاحيات التفصيلية تُحدد لحسابات المشرفين فقط.";
            return Redirect("/Admin/Users");
        }

        ViewBag.AdminUser = user;
        ViewBag.TargetRoleLabel = AdminRolePolicy.GetLabel(targetRole);

        var permissions = await _context.AdminPagePermissions
            .AsNoTracking()
            .Where(x => x.AdminUserId == id && x.CanAccess)
            .Select(x => x.PagePath)
            .ToListAsync(cancellationToken);

        ViewBag.AllowedPaths = permissions;

        var selectablePages = AdminPagesCatalog.Pages
            .Where(x =>
                x.PagePath != AdminRolePolicy.UserManagementPagePath &&
                !AdminRolePolicy.AlwaysAllowedPagePaths.Contains(x.PagePath))
            .ToList();

        return View("~/Areas/Admin/Views/AdminUsers/Permissions.cshtml", selectablePages);
    }

    [HttpPost("/Admin/Users/Permissions/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Permissions(
        int id,
        string[]? allowedPaths,
        CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        if (actor == null || !AdminRolePolicy.CanManageUsers(actor.RoleCode))
            return Forbid();

        var user = await _context.AdminUsers.FindAsync(new object[] { id }, cancellationToken);
        if (user == null)
            return NotFound();

        var targetRole = await AdminRolePolicy.ResolveRoleAsync(_context, user, cancellationToken);
        if (!AdminRolePolicy.CanManageTarget(actor.RoleCode, targetRole) ||
            targetRole != AdminRolePolicy.Supervisor)
        {
            return Forbid();
        }

        var oldPermissions = await _context.AdminPagePermissions
            .Where(x => x.AdminUserId == id)
            .ToListAsync(cancellationToken);

        _context.AdminPagePermissions.RemoveRange(oldPermissions);

        var selected = (allowedPaths ?? Array.Empty<string>())
            .Where(x => !string.Equals(x, AdminRolePolicy.UserManagementPagePath, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var page in AdminPagesCatalog.Pages.Where(x => selected.Contains(x.PagePath)))
        {
            _context.AdminPagePermissions.Add(new AdminPagePermission
            {
                AdminUserId = id,
                SectionName = page.SectionName,
                PageName = page.PageName,
                PagePath = page.PagePath,
                CanAccess = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "تم حفظ صلاحيات المشرف بنجاح.";
        return Redirect($"/Admin/Users/Permissions/{id}");
    }

    [HttpPost("/Admin/Users/SendPasswordSetupLink/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPasswordSetupLink(int id, CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        if (actor == null || !AdminRolePolicy.CanManageUsers(actor.RoleCode))
            return Forbid();

        var user = await _context.AdminUsers.FindAsync(new object[] { id }, cancellationToken);
        if (user == null)
            return NotFound();

        var targetRole = await AdminRolePolicy.ResolveRoleAsync(_context, user, cancellationToken);
        if (!AdminRolePolicy.CanManageTarget(actor.RoleCode, targetRole))
            return Forbid();

        if (!PortalEmailPolicy.IsDeliverable(user.Email))
        {
            TempData["Error"] = "البريد الإلكتروني للحساب غير صالح. عدّل البريد أولًا ثم أعد المحاولة.";
            return Redirect("/Admin/Users");
        }

        var oldStamp = user.SecurityStamp;
        var oldMustChange = user.MustChangePassword;
        user.SecurityStamp = LoginSecurity.NewSecurityStamp();
        user.MustChangePassword = true;
        user.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _passwordSetup.IssueAsync(
                "Admin",
                user.Id,
                user.Email!,
                user.FullName,
                $"لوحة التحكم - {AdminRolePolicy.GetLabel(targetRole)}",
                actor.User.Id,
                HttpContext,
                cancellationToken);
            TempData["Success"] = $"تم إرسال رابط آمن لإعداد كلمة المرور إلى {user.Email}.";
        }
        catch (Exception ex)
        {
            user.SecurityStamp = oldStamp;
            user.MustChangePassword = oldMustChange;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync(CancellationToken.None);

            _logger.LogError(ex, "Unable to send password setup link for admin user {AdminUserId}", id);
            TempData["Error"] = "تعذر إرسال رابط إعداد كلمة المرور، وتمت استعادة حالة الحساب السابقة.";
        }

        return Redirect("/Admin/Users");
    }

    [HttpPost("/Admin/Users/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        if (actor == null || !AdminRolePolicy.CanManageUsers(actor.RoleCode))
            return Forbid();

        if (actor.User.Id == id)
        {
            TempData["Error"] = "لا يمكن حذف حسابك الحالي.";
            return Redirect("/Admin/Users");
        }

        var user = await _context.AdminUsers.FindAsync(new object[] { id }, cancellationToken);
        if (user == null)
            return NotFound();

        var targetRole = await AdminRolePolicy.ResolveRoleAsync(_context, user, cancellationToken);
        if (!AdminRolePolicy.CanManageTarget(actor.RoleCode, targetRole))
            return Forbid();

        if (targetRole == AdminRolePolicy.SystemManager)
        {
            var otherActiveSystemManagers = await _context.AdminUsers
                .CountAsync(x => x.Id != user.Id && x.IsSuperAdmin && x.IsActive, cancellationToken);

            if (otherActiveSystemManagers == 0)
            {
                TempData["Error"] = "لا يمكن حذف آخر مدير نظام فعال.";
                return Redirect("/Admin/Users");
            }
        }

        var permissions = await _context.AdminPagePermissions
            .Where(x => x.AdminUserId == id)
            .ToListAsync(cancellationToken);

        _context.AdminPagePermissions.RemoveRange(permissions);
        _context.AdminUsers.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "تم حذف المستخدم بنجاح.";
        return Redirect("/Admin/Users");
    }

    private async Task<AdminActor?> GetActorAsync(CancellationToken cancellationToken)
    {
        var value = User.Claims.FirstOrDefault(x => x.Type == "KafoAdminUserId")?.Value;
        if (!int.TryParse(value, out var id))
            return null;

        var user = await _context.AdminUsers
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (user == null)
            return null;

        var roleCode = await AdminRolePolicy.ResolveRoleAsync(_context, user, cancellationToken);
        return new AdminActor(user, roleCode);
    }

    private static string ResolveRole(AdminUser user, IReadOnlySet<int> administrationManagerIds)
    {
        if (user.IsSuperAdmin)
            return AdminRolePolicy.SystemManager;

        return administrationManagerIds.Contains(user.Id)
            ? AdminRolePolicy.AdministrationManager
            : AdminRolePolicy.Supervisor;
    }

    private static int RoleSortOrder(string roleCode)
        => roleCode switch
        {
            AdminRolePolicy.SystemManager => 0,
            AdminRolePolicy.AdministrationManager => 1,
            _ => 2
        };

    private sealed record AdminActor(AdminUser User, string RoleCode);
}
