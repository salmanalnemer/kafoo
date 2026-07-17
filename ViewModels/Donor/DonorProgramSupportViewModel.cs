using System.ComponentModel.DataAnnotations;
using Kafo.Web.Models;

namespace Kafo.Web.ViewModels.Donor;

public class DonorProgramSupportViewModel
{
    public int ProgramProjectId { get; set; }
    public ProgramProject? Program { get; set; }

    public IReadOnlyList<BankAccount> BankAccounts { get; set; } = Array.Empty<BankAccount>();

    [Required(ErrorMessage = "قيمة الدعم مطلوبة")]
    [Range(1, 999999999, ErrorMessage = "قيمة الدعم يجب أن تكون أكبر من صفر")]
    [Display(Name = "قيمة الدعم")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "رقم العملية مطلوب")]
    [MaxLength(100, ErrorMessage = "رقم العملية يجب ألا يتجاوز 100 حرف")]
    [RegularExpression(@"^[A-Za-z0-9\-_/ ]+$", ErrorMessage = "رقم العملية يحتوي على أحرف غير مسموحة")]
    [Display(Name = "رقم العملية / مرجع التحويل")]
    public string TransactionNumber { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "الملاحظات طويلة جدًا")]
    [Display(Name = "ملاحظات اختيارية")]
    public string? Notes { get; set; }
}
