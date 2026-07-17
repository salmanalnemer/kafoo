using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class GeneralAssemblyMember
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم العضو مطلوب")]
    [MaxLength(220)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(160)]
    public string MembershipType { get; set; } = "عضو جمعية عمومية";

    [MaxLength(160)]
    public string? PositionTitle { get; set; }

    [MaxLength(120)]
    public string? MembershipNumber { get; set; }

    [MaxLength(120)]
    public string? TermLabel { get; set; }

    [MaxLength(500)]
    public string? PhotoPath { get; set; }

    [MaxLength(900)]
    public string? Bio { get; set; }

    public DateTime? JoinedAt { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
