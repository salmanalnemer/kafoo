using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.ViewModels.Admin;

public sealed class AdminAccountSettingsIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public string AccountTypeFilter { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = string.Empty;
    public IReadOnlyList<AdminPortalAccountRowViewModel> Accounts { get; set; } = Array.Empty<AdminPortalAccountRowViewModel>();
    public int TotalAccounts { get; set; }
    public int TotalDonors { get; set; }
    public int TotalOrganizations { get; set; }
    public int ActiveAccounts { get; set; }
    public int AccountsNeedingEmailUpdate { get; set; }
}

public sealed class AdminPortalAccountRowViewModel
{
    public int Id { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string AccountTypeLabel { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? SecondaryName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public bool EmailNeedsAttention { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string DetailsUrl { get; set; } = string.Empty;
}

public sealed class AdminPortalAccountUpdateViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "معرّف الحساب غير صحيح")]
    public int Id { get; set; }

    [Required(ErrorMessage = "نوع الحساب مطلوب")]
    public string AccountType { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم الحساب مطلوب")]
    [MaxLength(220, ErrorMessage = "اسم الحساب طويل جدًا")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب للدخول وإرسال رمز التحقق")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; }
}

public sealed class AdminPortalPasswordResetViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "معرّف الحساب غير صحيح")]
    public int Id { get; set; }

    [Required(ErrorMessage = "نوع الحساب مطلوب")]
    public string AccountType { get; set; } = string.Empty;
}
