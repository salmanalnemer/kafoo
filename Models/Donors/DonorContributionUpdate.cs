using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models.Donors;

public class DonorContributionUpdate
{
    public int Id { get; set; }

    public int DonorContributionId { get; set; }
    public DonorContribution? DonorContribution { get; set; }

    [Required]
    [MaxLength(220)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1200)]
    public string? Details { get; set; }

    public int? ProgressPercent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
