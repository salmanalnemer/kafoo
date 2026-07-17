using Kafo.Web.Models.Organizations;

namespace Kafo.Web.ViewModels.Organizations;

public class OrganizationDashboardViewModel
{
    public string OrganizationName { get; set; } = "الجهة";
    public string? OrganizationCity { get; set; }

    public int RequestsCount { get; set; }
    public int ActiveRequestsCount { get; set; }
    public int PendingReviewRequestsCount { get; set; }
    public int CompletedRequestsCount { get; set; }
    public int ClosedRequestsCount { get; set; }
    public int RequestsThisMonth { get; set; }

    public int JobsCount { get; set; }
    public int TrainingCount { get; set; }
    public int VolunteerCount { get; set; }
    public int TotalAvailableOpportunities { get; set; }

    public int CandidatesCount { get; set; }
    public int NewCandidatesCount { get; set; }
    public int InterviewCandidatesCount { get; set; }
    public int AcceptedCandidatesCount { get; set; }
    public int RejectedCandidatesCount { get; set; }
    public int CandidatesThisMonth { get; set; }

    public double SuccessRate { get; set; }
    public double FulfillmentRate { get; set; }
    public double AverageCandidatesPerRequest { get; set; }
    public int ProfileCompletion { get; set; }

    public int UnreadNotificationsCount { get; set; }

    public Dictionary<string, int> RequestStatusCounts { get; set; } = new();

    public List<OpportunityRequest> LatestRequests { get; set; } = new();
    public List<OrganizationNotification> LatestNotifications { get; set; } = new();
}
