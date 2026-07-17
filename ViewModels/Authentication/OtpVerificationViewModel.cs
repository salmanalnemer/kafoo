using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.ViewModels.Authentication;

public sealed class OtpVerificationViewModel
{
    [Required]
    public string ChallengeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز التحقق مطلوب")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "رمز التحقق يجب أن يتكون من 6 أرقام")]
    [Display(Name = "رمز التحقق")]
    public string Code { get; set; } = string.Empty;

    public string MaskedEmail { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
