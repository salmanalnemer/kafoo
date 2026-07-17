using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class FeedbackEntry
{
    public int Id { get; set; }

    [MaxLength(220)]
    public string? FullName { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    [MaxLength(180)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "نوع التغذية الراجعة مطلوب")]
    [MaxLength(140)]
    public string FeedbackType { get; set; } = "ملاحظة";

    [MaxLength(220)]
    public string? RelatedService { get; set; }

    [Required(ErrorMessage = "عنوان التغذية الراجعة مطلوب")]
    [MaxLength(260)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "نص التغذية الراجعة مطلوب")]
    [MaxLength(2500)]
    public string FeedbackBody { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "التقييم يجب أن يكون من 1 إلى 5")]
    public int Rating { get; set; } = 5;

    [MaxLength(500)]
    public string? AttachmentPath { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = "جديدة";

    [MaxLength(1500)]
    public string? AdminNotes { get; set; }

    public bool IsRead { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
