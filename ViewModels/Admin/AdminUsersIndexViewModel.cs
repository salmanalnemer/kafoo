using Kafo.Web.Models;

namespace Kafo.Web.ViewModels.Admin;

public sealed class AdminUsersIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public string CurrentUserRoleCode { get; set; } = string.Empty;
    public IReadOnlyList<AdminUserListItemViewModel> Users { get; set; } = Array.Empty<AdminUserListItemViewModel>();
}

public sealed class AdminUserListItemViewModel
{
    public required AdminUser User { get; init; }
    public string RoleCode { get; init; } = string.Empty;
    public string RoleLabel { get; init; } = string.Empty;
    public bool CanEdit { get; init; }
    public bool CanDelete { get; init; }
    public bool CanManagePermissions { get; init; }
}
