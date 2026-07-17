using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models.Organizations;

public class OrganizationEvaluation
{
    public int Id { get; set; }

    public int OrganizationAccountId { get; set; }
    public OrganizationAccount? OrganizationAccount { get; set; }

    public int? OpportunityRequestId { get; set; }
    public OpportunityRequest? OpportunityRequest { get; set; }

    [Range(1, 5)]
    public int CandidateQualityRate { get; set; } = 5;

    [Range(1, 5)]
    public int ServiceRate { get; set; } = 5;

    [MaxLength(1500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
