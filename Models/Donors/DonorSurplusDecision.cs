using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models.Donors;

public class DonorSurplusDecision
{
    public int Id { get; set; }

    public int DonorContributionId { get; set; }
    public DonorContribution? DonorContribution { get; set; }

    [Range(0, 999999999)]
    public decimal SurplusAmount { get; set; }

    [Required]
    [MaxLength(120)]
    public string DecisionType { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = "موافق عليه";

    [MaxLength(80)]
    public string? IpAddress { get; set; }

    public DateTime ApprovedAt { get; set; } = DateTime.Now;
}
