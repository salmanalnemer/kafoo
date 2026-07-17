using System.ComponentModel.DataAnnotations;
using Kafo.Web.Models.Organizations;

namespace Kafo.Web.ViewModels.Admin;

public class AdminOrganizationsIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = string.Empty;
    public List<OrganizationAccount> Organizations { get; set; } = new();
    public List<OpportunityRequest> PendingRequests { get; set; } = new();
    public int TotalOrganizations { get; set; }
    public int ActiveOrganizations { get; set; }
    public int TotalRequests { get; set; }
    public int PendingRequestsCount { get; set; }
    public int TotalCandidates { get; set; }
    public int AcceptedCandidates { get; set; }
}

public class AdminOrganizationFormViewModel
{
    public int Id { get; set; }
    [Required(ErrorMessage = "اسم الجهة مطلوب")]
    [MaxLength(220)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? LogoPath { get; set; }
    [MaxLength(180)] public string? Activity { get; set; }
    [MaxLength(120)] public string? City { get; set; }
    [MaxLength(180)] public string? ContactName { get; set; }
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب لتسجيل الدخول وإرسال رمز التحقق")]
    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
    [MaxLength(180)] public string Email { get; set; } = string.Empty;
    [MaxLength(40)] public string? Phone { get; set; }
    [MinLength(12, ErrorMessage = "كلمة المرور يجب ألا تقل عن 12 حرفًا")]
    [MaxLength(128)]
    public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AdminOrganizationDetailsViewModel
{
    public OrganizationAccount Organization { get; set; } = null!;
    public List<OpportunityRequest> Requests { get; set; } = new();
    public List<OpportunityCandidate> Candidates { get; set; } = new();
    public List<OrganizationNotification> Notifications { get; set; } = new();
    public List<OrganizationEvaluation> Evaluations { get; set; } = new();
    public int TotalOpportunities { get; set; }
    public int AcceptedCandidates { get; set; }
    public double SuccessRate { get; set; }
}
