using System.ComponentModel.DataAnnotations;
using Kafo.Web.Models.Donors;

namespace Kafo.Web.ViewModels.Admin;

public class AdminDonorsIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = string.Empty;
    public IReadOnlyList<DonorAccount> Donors { get; set; } = new List<DonorAccount>();
    public IReadOnlyList<DonorContribution> PendingContributions { get; set; } = new List<DonorContribution>();
    public int TotalDonors { get; set; }
    public int ActiveDonors { get; set; }
    public int PendingSupportRequests { get; set; }
    public int OpenContributions { get; set; }
    public decimal TotalSupportAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int UnreadNotifications { get; set; }
}

public class AdminDonorDetailsViewModel
{
    public DonorAccount Donor { get; set; } = new();
    public IReadOnlyList<DonorContribution> Contributions { get; set; } = new List<DonorContribution>();
    public IReadOnlyList<DonorNotification> Notifications { get; set; } = new List<DonorNotification>();
    public IReadOnlyList<DonorReport> Reports { get; set; } = new List<DonorReport>();
    public IReadOnlyList<DonorSurplusDecision> SurplusDecisions { get; set; } = new List<DonorSurplusDecision>();
    public decimal TotalAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int BeneficiariesCount { get; set; }
    public double AverageProgress { get; set; }
    public int PendingRequests { get; set; }
}

public class AdminDonorAccountFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم الداعم مطلوب")]
    [MaxLength(180, ErrorMessage = "اسم الداعم طويل جداً")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "نوع الداعم مطلوب")]
    [MaxLength(80)]
    public string DonorType { get; set; } = "فرد";

    [MaxLength(180)]
    public string? OrganizationName { get; set; }

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب لتسجيل الدخول وإرسال رمز التحقق")]
    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Phone { get; set; }

    [DataType(DataType.Password)]
    [MinLength(12, ErrorMessage = "كلمة المرور يجب ألا تقل عن 12 حرفًا")]
    [MaxLength(128)]
    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AdminDonorContributionFormViewModel
{
    public int Id { get; set; }
    public int DonorAccountId { get; set; }

    public int? ProgramProjectId { get; set; }

    [Required(ErrorMessage = "عنوان المساهمة مطلوب")]
    [MaxLength(220)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "حالة المساهمة مطلوبة")]
    [MaxLength(80)]
    public string Status { get; set; } = "قيد التنفيذ";

    [Range(0, 100, ErrorMessage = "نسبة الإنجاز يجب أن تكون بين 0 و 100")]
    public int ProgressPercent { get; set; }

    [Range(0, 999999999, ErrorMessage = "قيمة الدعم غير صحيحة")]
    public decimal TotalAmount { get; set; }

    [Range(0, 999999999, ErrorMessage = "المبلغ المصروف غير صحيح")]
    public decimal SpentAmount { get; set; }

    [Range(0, 999999999, ErrorMessage = "المبلغ المتبقي غير صحيح")]
    public decimal RemainingAmount { get; set; }

    [Range(0, 9999999, ErrorMessage = "عدد المستفيدين غير صحيح")]
    public int BeneficiariesCount { get; set; }

    [MaxLength(1000)]
    public string? ImpactSummary { get; set; }

    public bool HasSurplus { get; set; }
    public bool IsSurplusLocked { get; set; } = true;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
