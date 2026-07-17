using System.ComponentModel.DataAnnotations;
using Kafo.Web.Security;

namespace Kafo.Web.ViewModels.Admin;

public class AdminUserFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    [MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم المستخدم للدخول مطلوب")]
    [MaxLength(80)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب لتسجيل الدخول وإرسال رمز التحقق")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Phone { get; set; }

    [MinLength(12, ErrorMessage = "كلمة المرور يجب ألا تقل عن 12 حرفًا")]
    [MaxLength(128)]
    public string? Password { get; set; }

    [Required(ErrorMessage = "حدد نوع المستخدم")]
    [RegularExpression(
        "^(SystemManager|AdministrationManager|Supervisor)$",
        ErrorMessage = "نوع المستخدم غير صحيح")]
    public string RoleCode { get; set; } = AdminRolePolicy.Supervisor;

    public bool IsActive { get; set; } = true;
}
