using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models.Donors;

public class DonorReport
{
    public int Id { get; set; }

    public int DonorContributionId { get; set; }
    public DonorContribution? DonorContribution { get; set; }

    [Required]
    [MaxLength(220)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1200)]
    public string? Summary { get; set; }

    [MaxLength(500)]
    public string? FilePath { get; set; }

    [MaxLength(80)]
    public string ReportType { get; set; } = "تقرير دوري";

    public DateTime ReportDate { get; set; } = DateTime.Now;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
