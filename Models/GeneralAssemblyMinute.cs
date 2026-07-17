using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class GeneralAssemblyMinute
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان المحضر مطلوب")]
    [MaxLength(260)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(160)]
    public string MeetingType { get; set; } = "اجتماع الجمعية العمومية";

    [MaxLength(120)]
    public string? MeetingNumber { get; set; }

    [MaxLength(120)]
    public string? FiscalYear { get; set; }

    public DateTime MeetingDate { get; set; } = DateTime.Now;

    public int AttendeesCount { get; set; }

    [MaxLength(900)]
    public string? Summary { get; set; }

    [MaxLength(500)]
    public string? FilePath { get; set; }

    [MaxLength(500)]
    public string? CoverImagePath { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
