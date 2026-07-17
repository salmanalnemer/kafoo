using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.ViewModels.Donor;

public class DonorProfileViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم الداعم مطلوب")]
    [MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string DonorType { get; set; } = "فرد";

    [MaxLength(180)]
    public string? OrganizationName { get; set; }

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب لتسجيل الدخول وإرسال رمز التحقق")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Phone { get; set; }

    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
