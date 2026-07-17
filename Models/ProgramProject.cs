using System.ComponentModel.DataAnnotations;
using Kafo.Web.Security;

namespace Kafo.Web.Models;

public class ProgramProject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم البرنامج أو المشروع مطلوب")]
    [MaxLength(220)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "الوصف المختصر مطلوب")]
    [MaxLength(1200)]
    public string Description { get; set; } = string.Empty;

    public string? Details { get; set; }

    [MaxLength(500)]
    public string? ImagePath { get; set; }

    [MaxLength(100)]
    public string Category { get; set; } = "برنامج";

    [MaxLength(220)]
    public string? Beneficiaries { get; set; }

    [MaxLength(120)]
    public string? BeneficiariesCount { get; set; }

    [MaxLength(180)]
    public string? Sector { get; set; }

    [MaxLength(500)]
    [HttpsUrl]
    public string? ExternalUrl { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
