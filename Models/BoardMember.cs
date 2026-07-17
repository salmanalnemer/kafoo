using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class BoardMember
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم العضو مطلوب")]
    [MaxLength(220)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(160)]
    public string PositionTitle { get; set; } = "عضو مجلس الإدارة";

    [MaxLength(160)]
    public string? MembershipRole { get; set; }

    [MaxLength(120)]
    public string? BoardTerm { get; set; }

    public DateTime? TermStartDate { get; set; }

    public DateTime? TermEndDate { get; set; }

    [MaxLength(80)]
    public string? Phone { get; set; }

    [MaxLength(180)]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? PhotoPath { get; set; }

    [MaxLength(1000)]
    public string? Bio { get; set; }

    public bool IsChairman { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
