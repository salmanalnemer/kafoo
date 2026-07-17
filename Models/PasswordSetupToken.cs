using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public sealed class PasswordSetupToken
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string AccountType { get; set; } = string.Empty;

    public int AccountId { get; set; }

    [Required, MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? RequestedByAdminUserId { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }
}
