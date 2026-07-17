using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models.Donors;

public class DonorContributionCertificate
{
    public int Id { get; set; }

    public int DonorContributionId { get; set; }
    public DonorContribution? DonorContribution { get; set; }

    [Required]
    [MaxLength(40)]
    public string CertificateNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(220)]
    public string DonorName { get; set; } = string.Empty;

    [MaxLength(220)]
    public string? DonorOrganizationName { get; set; }

    [Required]
    [MaxLength(220)]
    public string ContributionTitle { get; set; } = string.Empty;

    [MaxLength(220)]
    public string? ProgramTitle { get; set; }

    [MaxLength(60)]
    public string? ContributionCode { get; set; }

    [Range(0, 999999999)]
    public decimal TotalAmount { get; set; }

    [Range(0, 999999999)]
    public decimal SpentAmount { get; set; }

    [Range(0, 999999999)]
    public decimal RemainingAmount { get; set; }

    public int BeneficiariesCount { get; set; }

    [MaxLength(1200)]
    public string? ImpactSummary { get; set; }

    [Required]
    [MaxLength(180)]
    public string ExecutiveManagerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(180)]
    public string ExecutiveManagerTitle { get; set; } = "المدير التنفيذي";

    [MaxLength(500)]
    public string? SignatureImagePath { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.Now;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
