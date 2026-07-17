using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.ViewModels.Admin;

public class AdminLoginViewModel
{
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [DataType(DataType.Password)]
    [StringLength(256, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
