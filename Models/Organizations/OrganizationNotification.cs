using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models.Organizations;

public class OrganizationNotification
{
    public int Id { get; set; }

    public int OrganizationAccountId { get; set; }
    public OrganizationAccount? OrganizationAccount { get; set; }

    public int? OpportunityRequestId { get; set; }
    public OpportunityRequest? OpportunityRequest { get; set; }

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
