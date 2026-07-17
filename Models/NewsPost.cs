using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class NewsPost
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان الخبر مطلوب")]
    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(900)]
    public string? Summary { get; set; }

    public string? Details { get; set; }

    [MaxLength(500)]
    public string? ImagePath { get; set; }

    [MaxLength(120)]
    public string? Category { get; set; }

    [MaxLength(120)]
    public string? Audience { get; set; }

    public DateTime PublishDate { get; set; } = DateTime.Now;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
