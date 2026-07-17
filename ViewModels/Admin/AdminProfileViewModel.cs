using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.ViewModels.Admin;

public class AdminProfileViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "الاسم مطلوب")]
    [MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    [MaxLength(80)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب لتسجيل الدخول وإرسال رمز التحقق")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Phone { get; set; }

    public string? ProfileImagePath { get; set; }

    public bool IsSuperAdmin { get; set; }

    public string RoleCode { get; set; } = string.Empty;

    public string RoleLabel { get; set; } = string.Empty;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
