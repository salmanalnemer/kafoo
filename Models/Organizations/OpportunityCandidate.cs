using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models.Organizations;

public class OpportunityCandidate
{
    public int Id { get; set; }

    public int OpportunityRequestId { get; set; }
    public OpportunityRequest? OpportunityRequest { get; set; }

    [Required]
    [MaxLength(180)]
    public string CandidateName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? CvFilePath { get; set; }

    [MaxLength(1200)]
    public string? Qualifications { get; set; }

    [MaxLength(1200)]
    public string? Skills { get; set; }

    [MaxLength(1200)]
    public string? OrganizationNotes { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = "مرشح جديد";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
