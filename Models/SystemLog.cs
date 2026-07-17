using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class SystemLog
{
    public long Id { get; set; }

    [Required, MaxLength(80)]
    public string EventType { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Severity { get; set; } = "Information";

    [MaxLength(30)]
    public string? ActorType { get; set; }

    [MaxLength(80)]
    public string? ActorId { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(500)]
    public string? Path { get; set; }

    [Required, MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    public bool Success { get; set; }

    [MaxLength(64)]
    public string? CorrelationId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
