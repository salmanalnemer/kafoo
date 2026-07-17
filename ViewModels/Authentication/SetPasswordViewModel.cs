using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.ViewModels.Authentication;

public sealed class SetPasswordViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    public string AccountLabel { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "تأكيد كلمة المرور غير مطابق")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
