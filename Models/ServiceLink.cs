using System.ComponentModel.DataAnnotations;
using Kafo.Web.Security;

namespace Kafo.Web.Models;

public class ServiceLink
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان الخدمة مطلوب")]
    [MaxLength(220)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "وصف الخدمة مطلوب")]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(600)]
    [SafeNavigationUrl]
    public string Url { get; set; } = string.Empty;

    [MaxLength(30)]
    public string LinkType { get; set; } = "External";

    [MaxLength(300)]
    public string? InternalPath { get; set; }

    [MaxLength(120)]
    public string Category { get; set; } = "خدمة إلكترونية";

    [MaxLength(180)]
    public string? TargetAudience { get; set; }

    [MaxLength(80)]
    public string ButtonText { get; set; } = "الدخول للخدمة";

    [MaxLength(500)]
    public string? ImagePath { get; set; }

    public bool OpenInNewTab { get; set; } = true;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
