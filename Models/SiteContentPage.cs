using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class SiteContentPage
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string PageKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "عنوان الصفحة مطلوب")]
    [MaxLength(220)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(350)]
    public string? Subtitle { get; set; }

    [Required(ErrorMessage = "محتوى الصفحة مطلوب")]
    public string Content { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImagePath { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
