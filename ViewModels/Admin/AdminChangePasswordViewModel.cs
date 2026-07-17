using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.ViewModels.Admin;

public class AdminChangePasswordViewModel
{
    [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [MinLength(12, ErrorMessage = "كلمة المرور يجب ألا تقل عن 12 حرفًا")]
    [MaxLength(128)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
    [Compare(nameof(NewPassword), ErrorMessage = "تأكيد كلمة المرور غير مطابق")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
