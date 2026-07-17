using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class AidReport
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان التقرير مطلوب")]
    [MaxLength(260)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(160)]
    public string ReportType { get; set; } = "تقرير مساعدات";

    [MaxLength(160)]
    public string PeriodLabel { get; set; } = string.Empty;

    public int BeneficiariesCount { get; set; }

    public int FamiliesCount { get; set; }

    public decimal TotalAmount { get; set; }

    [MaxLength(900)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? FilePath { get; set; }

    [MaxLength(500)]
    public string? CoverImagePath { get; set; }

    public DateTime PublishedAt { get; set; } = DateTime.Now;

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
