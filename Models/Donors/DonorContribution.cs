using System.ComponentModel.DataAnnotations;
using Kafo.Web.Models;

namespace Kafo.Web.Models.Donors;

public class DonorContribution
{
    public int Id { get; set; }

    public string? ContributionCode { get; set; }

    public int DonorAccountId { get; set; }
    public DonorAccount? DonorAccount { get; set; }

    public int? ProgramProjectId { get; set; }
    public ProgramProject? ProgramProject { get; set; }

    [Required(ErrorMessage = "عنوان المساهمة مطلوب")]
    [MaxLength(220)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Status { get; set; } = "قيد التنفيذ";

    [Range(0, 100)]
    public int ProgressPercent { get; set; }

    [Range(0, 999999999)]
    public decimal TotalAmount { get; set; }

    [Range(0, 999999999)]
    public decimal SpentAmount { get; set; }

    [Range(0, 999999999)]
    public decimal RemainingAmount { get; set; }

    [MaxLength(100)]
    public string? TransactionNumber { get; set; }

    public int BeneficiariesCount { get; set; }

    [MaxLength(1000)]
    public string? ImpactSummary { get; set; }

    public bool HasSurplus { get; set; }

    public bool IsSurplusLocked { get; set; } = true;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<DonorContributionUpdate> Updates { get; set; } = new List<DonorContributionUpdate>();
    public ICollection<DonorReport> Reports { get; set; } = new List<DonorReport>();
    public ICollection<DonorSurplusDecision> SurplusDecisions { get; set; } = new List<DonorSurplusDecision>();

    public DonorContributionCertificate? Certificate { get; set; }
}

