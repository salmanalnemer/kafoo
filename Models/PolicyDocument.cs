using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class PolicyDocument
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان الملف مطلوب")]
    [MaxLength(260)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "السنة مطلوبة")]
    [MaxLength(20)]
    public string Year { get; set; } = DateTime.Now.Year.ToString();

    [Required(ErrorMessage = "التصنيف مطلوب")]
    [MaxLength(160)]
    public string Category { get; set; } = "السياسات واللوائح";

    [MaxLength(900)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? FilePath { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
