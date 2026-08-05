using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.ViewModels.Authentication;

public sealed class PasswordResetOtpViewModel
{
    [Required]
    public string ChallengeId { get; set; } = string.Empty;

    public string PortalType { get; set; } = "Portal";
    public string MaskedEmail { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }

    [Required(ErrorMessage = "رمز التحقق مطلوب")]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "رمز التحقق يجب أن يتكون من 6 أرقام")]
    [Display(Name = "رمز التحقق")]
    public string Code { get; set; } = string.Empty;
}
