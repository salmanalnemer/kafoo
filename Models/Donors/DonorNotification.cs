using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models.Donors;

public class DonorNotification
{
    public int Id { get; set; }

    public int DonorAccountId { get; set; }
    public DonorAccount? DonorAccount { get; set; }

    public int? DonorContributionId { get; set; }
    public DonorContribution? DonorContribution { get; set; }

    [Required]
    [MaxLength(220)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1200)]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public bool SentBySms { get; set; }

    public bool SentByEmail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
