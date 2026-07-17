using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models.Organizations;

public class OpportunityRequest
{
    public int Id { get; set; }

    public int OrganizationAccountId { get; set; }
    public OrganizationAccount? OrganizationAccount { get; set; }

    #region معلومات الفرصة

    [Required(ErrorMessage = "نوع الفرصة مطلوب")]
    [MaxLength(80)]
    public string OpportunityType { get; set; } = "توظيف";

    [Required(ErrorMessage = "المسمى الوظيفي مطلوب")]
    [MaxLength(220)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? JobReferenceNumber { get; set; }

    [MaxLength(180)]
    public string? DepartmentName { get; set; }

    [Range(1, 10000)]
    public int AvailableCount { get; set; } = 1;

    public DateTime? ApplicationStartDate { get; set; }
    public DateTime? ApplicationEndDate { get; set; }

    #endregion

    #region الوصف الوظيفي

    [Required(ErrorMessage = "الوصف الوظيفي مطلوب")]
    [MaxLength(5000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(5000)]
    public string? Responsibilities { get; set; }

    [MaxLength(3000)]
    public string? JobObjectives { get; set; }

    [MaxLength(3000)]
    public string? PerformanceIndicators { get; set; }

    #endregion

    #region مقر وطبيعة العمل

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(250)]
    public string? WorkLocation { get; set; }

    [Required]
    [MaxLength(80)]
    public string WorkNature { get; set; } = "حضوري";

    [MaxLength(80)]
    public string? EmploymentType { get; set; }

    [MaxLength(80)]
    public string? WorkShift { get; set; }

    [MaxLength(120)]
    public string? WorkHours { get; set; }

    [MaxLength(120)]
    public string? ContractType { get; set; }

    public int? ContractDurationMonths { get; set; }

    #endregion

    #region الراتب والمزايا

    [Range(typeof(decimal), "0", "10000000")]
    public decimal? SalaryAmount { get; set; }

    public decimal? SalaryFrom { get; set; }
    public decimal? SalaryTo { get; set; }
    public bool SalaryNegotiable { get; set; }

    [Range(0, 365)]
    public int? AnnualLeaveDays { get; set; }

    [MaxLength(3000)]
    public string? Benefits { get; set; }

    #endregion

    #region المؤهلات

    [MaxLength(150)]
    public string? EducationLevel { get; set; }

    [MaxLength(2500)]
    public string? Qualifications { get; set; }

    [MaxLength(2500)]
    public string? AcceptedSpecializations { get; set; }

    public decimal? MinimumGpa { get; set; }
    public int? MinimumExperienceYears { get; set; }

    [MaxLength(2000)]
    public string? ProfessionalCertificates { get; set; }

    #endregion

    #region المهارات

    [MaxLength(2500)]
    public string? Skills { get; set; }

    [MaxLength(2500)]
    public string? TechnicalSkills { get; set; }

    [MaxLength(2000)]
    public string? RequiredCourses { get; set; }

    [MaxLength(1500)]
    public string? Languages { get; set; }

    [MaxLength(150)]
    public string? DrivingLicense { get; set; }

    #endregion

    #region ذوي الإعاقة

    public bool AcceptAllDisabilities { get; set; }

    [MaxLength(2500)]
    public string? SuitableDisabilityTypes { get; set; }

    [MaxLength(3000)]
    public string? WorkplaceAccommodations { get; set; }

    public bool SupportEmployeeAvailable { get; set; }
    public bool RemoteWorkAvailable { get; set; }
    public bool WorkplaceCanBeModified { get; set; }

    [MaxLength(3000)]
    public string? AssistiveTechnologies { get; set; }

    #endregion

    #region مسؤول التوظيف

    [MaxLength(180)]
    public string? RecruitmentOfficerName { get; set; }

    [EmailAddress]
    [MaxLength(180)]
    public string? RecruitmentOfficerEmail { get; set; }

    [Phone]
    [MaxLength(30)]
    public string? RecruitmentOfficerPhone { get; set; }

    #endregion

    #region المرفقات

    [MaxLength(500)]
    public string? JobDescriptionFile { get; set; }

    [MaxLength(500)]
    public string? AdditionalAttachment { get; set; }

    [MaxLength(500)]
    public string? OpportunityImage { get; set; }

    [MaxLength(500)]
    public string? IntroVideo { get; set; }

    #endregion

    #region أسئلة الفرز

    [MaxLength(8000)]
    public string? ScreeningQuestionsJson { get; set; }

    #endregion

    #region حالة الطلب

    [Required]
    [MaxLength(80)]
    public string Status { get; set; } = "جديد";

    [MaxLength(2000)]
    public string? OrganizationNotes { get; set; }

    [MaxLength(2000)]
    public string? AdminNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    #endregion

    public ICollection<OpportunityCandidate> Candidates { get; set; } = new List<OpportunityCandidate>();
}
