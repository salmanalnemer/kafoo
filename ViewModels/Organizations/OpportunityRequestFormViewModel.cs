using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.ViewModels.Organizations;

public class OpportunityRequestFormViewModel
{
    [Required(ErrorMessage = "نوع الفرصة مطلوب")]
    [Display(Name = "نوع الفرصة")]
    public string OpportunityType { get; set; } = "توظيف";

    [Required(ErrorMessage = "المسمى أو اسم الفرصة مطلوب")]
    [MaxLength(220)]
    [Display(Name = "المسمى الوظيفي أو اسم الفرصة")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "الوصف أو المهام مطلوبة")]
    [MaxLength(2500)]
    [Display(Name = "الوصف الوظيفي أو المهام")]
    public string Description { get; set; } = string.Empty;

    [Range(1, 10000, ErrorMessage = "عدد الفرص يجب أن يكون أكبر من صفر")]
    [Display(Name = "عدد الفرص")]
    public int AvailableCount { get; set; } = 1;

    [MaxLength(120)]
    [Display(Name = "المدينة")]
    public string? City { get; set; }

    [MaxLength(220)]
    [Display(Name = "مقر العمل")]
    public string? WorkLocation { get; set; }

    [MaxLength(1200)]
    [Display(Name = "المؤهل المطلوب")]
    public string? Qualifications { get; set; }

    [MaxLength(1200)]
    [Display(Name = "المهارات المطلوبة")]
    public string? Skills { get; set; }

    [MaxLength(1200)]
    [Display(Name = "أنواع الإعاقة المناسبة")]
    public string? SuitableDisabilityTypes { get; set; }

    [Required(ErrorMessage = "طبيعة العمل مطلوبة")]
    [Display(Name = "طبيعة العمل")]
    public string WorkNature { get; set; } = "حضوري";

    [MaxLength(80)]
    [Display(Name = "نوع الدوام")]
    public string? EmploymentType { get; set; }

    [MaxLength(180)]
    [Display(Name = "ساعات العمل أو مدة الفرصة")]
    public string? WorkHours { get; set; }

    [Range(typeof(decimal), "0", "10000000", ErrorMessage = "قيمة الراتب أو المكافأة غير صحيحة")]
    [Display(Name = "الراتب أو المكافأة الشهرية")]
    public decimal? SalaryAmount { get; set; }

    [Range(0, 365, ErrorMessage = "عدد أيام الإجازة يجب أن يكون بين 0 و365")]
    [Display(Name = "عدد أيام الإجازة السنوية")]
    public int? AnnualLeaveDays { get; set; }
}
