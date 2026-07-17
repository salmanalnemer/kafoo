using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class InKindDonationType
{
    public int Id { get; set; }

    [Required]
    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Icon { get; set; } = "▣";

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
