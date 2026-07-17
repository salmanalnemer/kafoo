using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class PresidentMessage
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم الرئيس مطلوب")]
    [MaxLength(180)]
    public string LeaderName { get; set; } = string.Empty;

    [Required(ErrorMessage = "المنصب مطلوب")]
    [MaxLength(180)]
    public string PositionTitle { get; set; } = "رئيس مجلس الإدارة";

    [Required(ErrorMessage = "عنوان الكلمة مطلوب")]
    [MaxLength(220)]
    public string Title { get; set; } = "كلمة رئيس الجمعية";

    [Required(ErrorMessage = "نص الكلمة مطلوب")]
    public string MessageText { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImagePath { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
