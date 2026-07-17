using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class ExecutiveManagerMessage
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم المدير التنفيذي مطلوب")]
    [MaxLength(180)]
    public string ManagerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "المنصب مطلوب")]
    [MaxLength(180)]
    public string PositionTitle { get; set; } = "المدير التنفيذي";

    [Required(ErrorMessage = "عنوان الصفحة مطلوب")]
    [MaxLength(220)]
    public string Title { get; set; } = "كلمة المدير التنفيذي";

    [Required(ErrorMessage = "نص الكلمة مطلوب")]
    public string MessageText { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImagePath { get; set; }

    [MaxLength(500)]
    public string? SignatureImagePath { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
