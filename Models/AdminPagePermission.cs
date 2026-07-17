using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class AdminPagePermission
{
    public int Id { get; set; }

    public int AdminUserId { get; set; }

    public AdminUser? AdminUser { get; set; }

    [Required]
    [MaxLength(160)]
    public string SectionName { get; set; } = string.Empty;

    [Required]
    [MaxLength(160)]
    public string PageName { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string PagePath { get; set; } = string.Empty;

    public bool CanAccess { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
