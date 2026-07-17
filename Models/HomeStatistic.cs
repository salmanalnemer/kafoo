using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class HomeStatistic
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم الإحصائية مطلوب")]
    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "الرقم مطلوب")]
    [MaxLength(80)]
    public string Value { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Icon { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
