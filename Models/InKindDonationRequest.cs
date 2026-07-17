using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class InKindDonationRequest
{
    public int Id { get; set; }

    [Required]
    [MaxLength(180)]
    public string DonorName { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Mobile { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string City { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? District { get; set; }

    [Required]
    public string DonationTypeIds { get; set; } = string.Empty;

    [Required]
    public string DonationTypeNames { get; set; } = string.Empty;

    [MaxLength(30)]
    public string PreferredPickupTime { get; set; } = "morning";

    [MaxLength(1200)]
    public string? Notes { get; set; }

    [MaxLength(60)]
    public string Status { get; set; } = "جديد";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
