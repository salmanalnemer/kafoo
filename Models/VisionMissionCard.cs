using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class VisionMissionCard
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان البطاقة مطلوب")]
    [MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "المحتوى مطلوب")]
    public string Content { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Icon { get; set; } = "fa-solid fa-eye";

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
