using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class StrategicGoal
{
    public int Id { get; set; }

    [Required(ErrorMessage = "العنوان مطلوب")]
    [MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(700)]
    public string? Description { get; set; }

    [MaxLength(80)]
    public string? Icon { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
