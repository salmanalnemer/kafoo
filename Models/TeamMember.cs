using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class TeamMember
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم عضو الفريق مطلوب")]
    [MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "المسمى الوظيفي مطلوب")]
    [MaxLength(180)]
    public string JobTitle { get; set; } = string.Empty;

    [MaxLength(900)]
    public string? Bio { get; set; }

    [MaxLength(500)]
    public string? ImagePath { get; set; }

    [MaxLength(250)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
