using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class AdminUser
{
    public int Id { get; set; }

    [Required]
    [MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(180)]
    public string? Email { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? ProfileImagePath { get; set; }

    [Required]
    [MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string PasswordSalt { get; set; } = string.Empty;

    public bool IsSuperAdmin { get; set; }


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
}
