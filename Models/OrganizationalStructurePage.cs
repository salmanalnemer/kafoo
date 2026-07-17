using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class OrganizationalStructurePage
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان الصفحة مطلوب")]
    [MaxLength(220)]
    public string Title { get; set; } = "الهيكل التنظيمي";

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string? LayoutJson { get; set; }

    [MaxLength(500)]
    public string? ImagePath { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
