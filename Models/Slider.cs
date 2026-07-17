using System.ComponentModel.DataAnnotations;
using Kafo.Web.Security;

namespace Kafo.Web.Models;

public class Slider
{
    public int Id { get; set; }

    [MaxLength(250)]
    public string? Title { get; set; }

    [MaxLength(800)]
    public string? Description { get; set; }

    [MaxLength(120)]
    public string? ButtonText { get; set; }

    [MaxLength(500)]
    [SafeNavigationUrl]
    public string? ButtonUrl { get; set; }

    [Required]
    [MaxLength(500)]
    public string ImagePath { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime? PublishStart { get; set; }

    public DateTime? PublishEnd { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}
