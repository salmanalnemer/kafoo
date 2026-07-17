using System.ComponentModel.DataAnnotations;
using Kafo.Web.Security;

namespace Kafo.Web.Models;

public class VolunteerOpportunity
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان الفرصة مطلوب")]
    [MaxLength(260)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(160)]
    public string OpportunityType { get; set; } = "فرصة تطوعية";

    [MaxLength(220)]
    public string? Location { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int VolunteerHours { get; set; }

    public int SeatsCount { get; set; }

    [MaxLength(900)]
    public string? Description { get; set; }

    [MaxLength(900)]
    public string? Requirements { get; set; }

    [MaxLength(800)]
    [HttpsUrl]
    public string? RegistrationUrl { get; set; }

    [MaxLength(500)]
    public string? ImagePath { get; set; }

    public bool IsRemote { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
