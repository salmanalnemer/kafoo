using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.ViewModels.Authentication;

public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;

    public string PortalType { get; set; } = "Portal";
}
