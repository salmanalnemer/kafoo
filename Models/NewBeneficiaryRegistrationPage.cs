using System.ComponentModel.DataAnnotations;
using Kafo.Web.Security;

namespace Kafo.Web.Models;

public class NewBeneficiaryRegistrationPage
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان الصفحة مطلوب")]
    [MaxLength(220)]
    public string Title { get; set; } = "تسجيل مستفيد جديد";

    [MaxLength(350)]
    public string Subtitle { get; set; } = "خدمة إلكترونية";

    [Required(ErrorMessage = "وصف الصفحة مطلوب")]
    public string Description { get; set; } = "يمكنك التسجيل كمستفيد جديد من خلال تعبئة البيانات وإرفاق المستندات المطلوبة.";

    [MaxLength(30)]
    public string VideoSourceType { get; set; } = "Youtube";

    [MaxLength(700)]
    [HttpsUrl]
    public string? YoutubeUrl { get; set; }

    [MaxLength(500)]
    public string? VideoPath { get; set; }

    [MaxLength(220)]
    public string AlertTitle { get; set; } = "تنبيه مهم";

    [MaxLength(900)]
    public string AlertText { get; set; } = "يشترط للتسجيل كمستفيد جديد تعبئة البيانات بدقة وإرفاق المستندات المطلوبة كاملة.";

    [MaxLength(120)]
    public string RegisterButtonText { get; set; } = "التسجيل كمستفيد جديد";

    [MaxLength(700)]
    [SafeNavigationUrl]
    public string RegisterButtonUrl { get; set; } = "#";

    public bool OpenRegisterInNewTab { get; set; } = true;

    [MaxLength(30)]
    public string? WhatsAppNumber { get; set; } = "966500000000";

    [MaxLength(120)]
    public string WhatsAppButtonText { get; set; } = "طلب مساعدة عبر واتساب";

    [MaxLength(500)]
    public string WhatsAppMessage { get; set; } = "السلام عليكم، أرغب بالتسجيل كمستفيد جديد وأحتاج المساعدة.";

    public bool ShowWhatsAppButton { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
