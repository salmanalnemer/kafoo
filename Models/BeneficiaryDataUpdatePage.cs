using System.ComponentModel.DataAnnotations;
using Kafo.Web.Security;

namespace Kafo.Web.Models;

public class BeneficiaryDataUpdatePage
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان الصفحة مطلوب")]
    [MaxLength(220)]
    public string Title { get; set; } = "تحديث بيانات المستفيدين";

    [MaxLength(350)]
    public string Subtitle { get; set; } = "خدمة إلكترونية";

    [Required(ErrorMessage = "وصف الصفحة مطلوب")]
    public string Description { get; set; } = "يمكن للمستفيد تحديث بياناته وإرفاق المستندات المطلوبة من خلال الرابط المعتمد، مع مشاهدة شرح مبسط لطريقة تحديث البيانات.";

    [MaxLength(30)]
    public string VideoSourceType { get; set; } = "Youtube";

    [MaxLength(700)]
    [HttpsUrl]
    public string? YoutubeUrl { get; set; }

    [MaxLength(500)]
    public string? VideoPath { get; set; }

    [MaxLength(220)]
    public string AlertTitle { get; set; } = "تنبيه مهم";

    [MaxLength(800)]
    public string AlertText { get; set; } = "يرجى التأكد من صحة البيانات وإرفاق المستندات المطلوبة قبل إرسال الطلب.";

    [MaxLength(120)]
    public string PrimaryButtonText { get; set; } = "تحديث بياناتي الآن";

    [MaxLength(700)]
    [SafeNavigationUrl]
    public string PrimaryButtonUrl { get; set; } = "/ServiceLinks";

    [MaxLength(120)]
    public string SecondaryButtonText { get; set; } = "العودة للخدمات";

    [MaxLength(700)]
    [SafeNavigationUrl]
    public string SecondaryButtonUrl { get; set; } = "/ServiceLinks";

    public bool OpenPrimaryInNewTab { get; set; } = true;

    [MaxLength(30)]
    public string? WhatsAppNumber { get; set; } = "966500000000";

    [MaxLength(120)]
    public string WhatsAppButtonText { get; set; } = "تواصل عبر واتساب";

    [MaxLength(500)]
    public string WhatsAppMessage { get; set; } = "السلام عليكم، أرغب بالمساعدة في تحديث بيانات المستفيد.";

    public bool ShowWhatsAppButton { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
