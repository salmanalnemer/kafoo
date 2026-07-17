using Kafo.Web.Data;
using Kafo.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Security;

public static class AdminRolePolicy
{
    public const string SystemManager = "SystemManager";
    public const string AdministrationManager = "AdministrationManager";
    public const string Supervisor = "Supervisor";

    public const string UserManagementPagePath = "/Admin/Users";
    public const string DashboardPagePath = "/Admin";
    public const string ProfilePagePath = "/Admin/Profile";

    public static IReadOnlySet<string> AlwaysAllowedPagePaths { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ProfilePagePath
        };

    public static IReadOnlyList<string> AllRoles { get; } =
        new[] { SystemManager, AdministrationManager, Supervisor };

    public static string GetLabel(string? roleCode)
        => roleCode switch
        {
            SystemManager => "مدير النظام",
            AdministrationManager => "مدير الإدارة",
            _ => "مشرف"
        };

    public static bool CanManageUsers(string? roleCode)
        => roleCode is SystemManager or AdministrationManager;

    public static bool HasFullPageAccess(string? roleCode)
        => roleCode is SystemManager or AdministrationManager;

    public static bool CanAssignRole(string? actorRoleCode, string? targetRoleCode)
    {
        if (actorRoleCode == SystemManager)
            return AllRoles.Contains(targetRoleCode ?? string.Empty, StringComparer.Ordinal);

        return actorRoleCode == AdministrationManager && targetRoleCode == Supervisor;
    }

    public static bool CanManageTarget(string? actorRoleCode, string? targetRoleCode)
    {
        if (actorRoleCode == SystemManager)
            return true;

        return actorRoleCode == AdministrationManager && targetRoleCode == Supervisor;
    }

    public static async Task<HashSet<int>> GetAdministrationManagerIdsAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        return (await db.AdminPagePermissions
                .AsNoTracking()
                .Where(x =>
                    x.CanAccess &&
                    x.PagePath == UserManagementPagePath)
                .Select(x => x.AdminUserId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();
    }

    public static async Task<string> ResolveRoleAsync(
        ApplicationDbContext db,
        AdminUser user,
        CancellationToken cancellationToken = default)
    {
        if (user.IsSuperAdmin)
            return SystemManager;

        var isAdministrationManager = await db.AdminPagePermissions
            .AsNoTracking()
            .AnyAsync(x =>
                x.AdminUserId == user.Id &&
                x.PagePath == UserManagementPagePath &&
                x.CanAccess,
                cancellationToken);

        return isAdministrationManager
            ? AdministrationManager
            : Supervisor;
    }

    public static async Task ApplyRoleAsync(
        ApplicationDbContext db,
        AdminUser user,
        string roleCode,
        bool clearPagePermissions,
        CancellationToken cancellationToken = default)
    {
        if (!AllRoles.Contains(roleCode, StringComparer.Ordinal))
            throw new ArgumentOutOfRangeException(nameof(roleCode), "نوع المستخدم غير صحيح.");

        user.IsSuperAdmin = roleCode == SystemManager;

        var currentPermissions = await db.AdminPagePermissions
            .Where(x => x.AdminUserId == user.Id)
            .ToListAsync(cancellationToken);

        if (clearPagePermissions)
            db.AdminPagePermissions.RemoveRange(currentPermissions);
        else
        {
            var userManagementPermissions = currentPermissions
                .Where(x => x.PagePath == UserManagementPagePath)
                .ToList();

            db.AdminPagePermissions.RemoveRange(userManagementPermissions);
        }

        if (roleCode == AdministrationManager)
        {
            db.AdminPagePermissions.Add(new AdminPagePermission
            {
                AdminUserId = user.Id,
                SectionName = "التواصل والإعدادات",
                PageName = "إدارة مستخدمي لوحة التحكم والصلاحيات",
                PagePath = UserManagementPagePath,
                CanAccess = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }
    }
}
