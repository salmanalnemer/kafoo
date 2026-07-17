using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public sealed class LoginOtpChallenge
{
    [Key, MaxLength(64)]
    public string ChallengeId { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string PortalType { get; set; } = string.Empty;

    public int AccountId { get; set; }

    [Required, MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(220)]
    public string DisplayName { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    [MaxLength(1000)]
    public string? ReturnUrl { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [Required, MaxLength(2000)]
    public string ProtectedCode { get; set; } = string.Empty;

    public int Attempts { get; set; }
    public int SendCount { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastSentAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
}
