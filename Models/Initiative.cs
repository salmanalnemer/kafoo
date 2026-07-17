using System.ComponentModel.DataAnnotations;
using Kafo.Web.Security;

namespace Kafo.Web.Models;

public class Initiative
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان المبادرة مطلوب")]
    [MaxLength(260)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(160)]
    public string InitiativeType { get; set; } = "مبادرة مجتمعية";

    [MaxLength(220)]
    public string? Location { get; set; }

    [MaxLength(220)]
    public string? TargetGroup { get; set; }

    public int BeneficiariesCount { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Objectives { get; set; }

    [MaxLength(800)]
    [HttpsUrl]
    public string? RegistrationUrl { get; set; }

    [MaxLength(500)]
    public string? ImagePath { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
