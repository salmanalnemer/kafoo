using System.ComponentModel.DataAnnotations;
using Kafo.Web.Security;

namespace Kafo.Web.Models;

public class SuccessPartner
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم الشريك مطلوب")]
    [MaxLength(220)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(180)]
    public string PartnerType { get; set; } = "شريك نجاح";

    [MaxLength(800)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? LogoPath { get; set; }

    [MaxLength(800)]
    [HttpsUrl]
    public string? WebsiteUrl { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
