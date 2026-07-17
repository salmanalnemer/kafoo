using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class NewBeneficiaryRegistrationRequirement
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم المستند مطلوب")]
    [MaxLength(220)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(700)]
    public string? Note { get; set; }

    [MaxLength(80)]
    public string Icon { get; set; } = "▣";

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
