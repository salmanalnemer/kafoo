using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class LicenseDocument
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان الترخيص مطلوب")]
    [MaxLength(220)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(900)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(20)]
    public string FileType { get; set; } = "image";

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
