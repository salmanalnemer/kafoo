using Kafo.Web.Models.Donors;

namespace Kafo.Web.ViewModels.Donor;

public class DonorDashboardViewModel
{
    public decimal TotalAmount { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalRemaining { get; set; }
    public int ContributionsCount { get; set; }
    public int ActiveContributionsCount { get; set; }
    public int CompletedContributionsCount { get; set; }
    public int ProgramsCount { get; set; }
    public int BeneficiariesCount { get; set; }
    public double AverageProgress { get; set; }
    public int ReportsCount { get; set; }
    public int PendingSurplusCount { get; set; }
    public int UnreadNotificationsCount { get; set; }
    public List<DonorContribution> LatestContributions { get; set; } = new();
    public List<DonorNotification> LatestNotifications { get; set; } = new();
    public List<DonorReport> LatestReports { get; set; } = new();
}
