using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models.Organizations;

public class OrganizationAccount
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم الجهة مطلوب")]
    [MaxLength(220)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LogoPath { get; set; }

    [MaxLength(180)]
    public string? Activity { get; set; }

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(180)]
    public string? ContactName { get; set; }

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

    public ICollection<OpportunityRequest> OpportunityRequests { get; set; } = new List<OpportunityRequest>();
    public ICollection<OrganizationNotification> Notifications { get; set; } = new List<OrganizationNotification>();
}
