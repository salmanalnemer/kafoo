using System.ComponentModel.DataAnnotations;
using Kafo.Web.Security;

namespace Kafo.Web.Models;

public class VideoLibraryItem
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان الفيديو مطلوب")]
    [MaxLength(260)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Category { get; set; } = "مكتبة الفيديو";

    [MaxLength(900)]
    public string? Description { get; set; }

    [MaxLength(30)]
    public string VideoSourceType { get; set; } = "Youtube";

    [MaxLength(800)]
    [HttpsUrl]
    public string? YoutubeUrl { get; set; }

    [MaxLength(500)]
    public string? VideoPath { get; set; }

    [MaxLength(500)]
    public string? ThumbnailPath { get; set; }

    public DateTime PublishedAt { get; set; } = DateTime.Now;

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
