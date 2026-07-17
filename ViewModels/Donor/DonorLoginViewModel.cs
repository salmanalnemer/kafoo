using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.ViewModels.Donor;

public class DonorLoginViewModel
{
    [Required(ErrorMessage = "البريد الإلكتروني أو رقم الجوال مطلوب")]
    [Display(Name = "البريد الإلكتروني أو رقم الجوال")]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
