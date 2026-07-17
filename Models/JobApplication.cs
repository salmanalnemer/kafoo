using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class JobApplication
{
    public int Id { get; set; }

    [Required(ErrorMessage = "الاسم الكامل مطلوب")]
    [MaxLength(220)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم الجوال مطلوب")]
    [MaxLength(40)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? NationalId { get; set; }

    [MaxLength(160)]
    public string? City { get; set; }

    [MaxLength(220)]
    public string? DesiredJobTitle { get; set; }

    [MaxLength(220)]
    public string? Qualification { get; set; }

    [MaxLength(220)]
    public string? Specialty { get; set; }

    public int ExperienceYears { get; set; }

    [MaxLength(1500)]
    public string? CoverLetter { get; set; }

    [MaxLength(500)]
    public string? CvFilePath { get; set; }

    [MaxLength(500)]
    public string? AttachmentFilePath { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = "جديد";

    [MaxLength(1500)]
    public string? AdminNotes { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
