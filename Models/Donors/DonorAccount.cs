using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models.Donors;

public class DonorAccount
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم الداعم مطلوب")]
    [MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string DonorType { get; set; } = "فرد";

    [MaxLength(180)]
    public string? OrganizationName { get; set; }

    [MaxLength(180)]
    public string? Email { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }

    [Required]
    [MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string PasswordSalt { get; set; } = string.Empty;


    [Required]
    [MaxLength(64)]
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public bool MustChangePassword { get; set; }

    public int AccessFailedCount { get; set; }

    public DateTime? LockoutEndUtc { get; set; }

    public DateTime? PasswordChangedAtUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<DonorContribution> Contributions { get; set; } = new List<DonorContribution>();
    public ICollection<DonorNotification> Notifications { get; set; } = new List<DonorNotification>();
}
