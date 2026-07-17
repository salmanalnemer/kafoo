using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class SatisfactionResponse
{
    public int Id { get; set; }

    [MaxLength(220)]
    public string? FullName { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    [MaxLength(180)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "نوع المستفيد مطلوب")]
    [MaxLength(140)]
    public string BeneficiaryType { get; set; } = "مستفيد";

    [Required(ErrorMessage = "اسم الخدمة مطلوب")]
    [MaxLength(220)]
    public string ServiceName { get; set; } = string.Empty;

    [Required(ErrorMessage = "مستوى الرضا مطلوب")]
    [MaxLength(80)]
    public string SatisfactionLevel { get; set; } = "راضٍ";

    [Range(1, 5, ErrorMessage = "التقييم يجب أن يكون من 1 إلى 5")]
    public int Rating { get; set; } = 5;

    [MaxLength(1200)]
    public string? PositiveNotes { get; set; }

    [MaxLength(1200)]
    public string? ImprovementNotes { get; set; }

    [MaxLength(1500)]
    public string? Suggestions { get; set; }

    public bool IsRead { get; set; }

    public bool IsArchived { get; set; }

    [MaxLength(1500)]
    public string? AdminNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
