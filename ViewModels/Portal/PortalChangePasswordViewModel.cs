using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.ViewModels.Portal;

public sealed class PortalChangePasswordViewModel
{
    [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [DataType(DataType.Password)]
    [MinLength(12, ErrorMessage = "كلمة المرور الجديدة يجب ألا تقل عن 12 حرفًا")]
    [MaxLength(128)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "تأكيد كلمة المرور غير مطابق")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
