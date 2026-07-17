using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class ContactMessage
{
    public int Id { get; set; }

    [Required(ErrorMessage = "الاسم مطلوب")]
    [MaxLength(220)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم الجوال مطلوب")]
    [MaxLength(40)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    [MaxLength(180)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "نوع الرسالة مطلوب")]
    [MaxLength(120)]
    public string MessageType { get; set; } = "استفسار";

    [Required(ErrorMessage = "عنوان الرسالة مطلوب")]
    [MaxLength(260)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "نص الرسالة مطلوب")]
    [MaxLength(2500)]
    public string MessageBody { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AttachmentPath { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = "جديدة";

    [MaxLength(1500)]
    public string? AdminNotes { get; set; }

    public bool IsRead { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
