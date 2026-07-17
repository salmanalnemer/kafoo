using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.ViewModels.Organizations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
public class OrganizationDashboardController : Controller
{
    private static readonly string[] RequestStages =
    {
        "جديد",
        "قيد المراجعة",
        "قيد الترشيح",
        "تم ترشيح المرشحين",
        "تم إجراء المقابلات",
        "تم التوظيف",
        "تم إغلاق الطلب"
    };

    private readonly ApplicationDbContext _context;

    public OrganizationDashboardController(
        ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Portal/Organization")]
    [HttpGet("/Portal/Organization/Dashboard")]
    public async Task<IActionResult> Index()
    {
        var organizationId = GetOrganizationId();
        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        var organization = await _context.OrganizationAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == organizationId);

        var requests = await _context.OpportunityRequests
            .AsNoTracking()
            .Include(x => x.Candidates)
            .Where(x => x.OrganizationAccountId == organizationId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var candidates = requests
            .SelectMany(x => x.Candidates)
            .ToList();

        var notificationsQuery = _context.OrganizationNotifications
            .AsNoTracking()
            .Where(x => x.OrganizationAccountId == organizationId);

        var requestsCount = requests.Count;
        var totalAvailableOpportunities = requests.Sum(x => x.AvailableCount);
        var acceptedCandidatesCount = candidates.Count(x => x.Status == "تم القبول");

        var profileFields = new[]
        {
            !string.IsNullOrWhiteSpace(organization?.Name),
            !string.IsNullOrWhiteSpace(organization?.LogoPath),
            !string.IsNullOrWhiteSpace(organization?.Activity),
            !string.IsNullOrWhiteSpace(organization?.City),
            !string.IsNullOrWhiteSpace(organization?.ContactName),
            !string.IsNullOrWhiteSpace(organization?.Email),
            !string.IsNullOrWhiteSpace(organization?.Phone)
        };

        var model = new OrganizationDashboardViewModel
        {
            OrganizationName = organization?.Name
                ?? User.Identity?.Name
                ?? "الجهة",
            OrganizationCity = organization?.City,

            RequestsCount = requestsCount,
            ActiveRequestsCount = requests.Count(x =>
                x.Status != "تم إغلاق الطلب" &&
                x.Status != "تم التوظيف"),
            PendingReviewRequestsCount = requests.Count(x =>
                x.Status == "جديد" ||
                x.Status == "قيد المراجعة"),
            CompletedRequestsCount = requests.Count(x =>
                x.Status == "تم التوظيف"),
            ClosedRequestsCount = requests.Count(x =>
                x.Status == "تم إغلاق الطلب"),
            RequestsThisMonth = requests.Count(x =>
                x.CreatedAt >= monthStart),

            JobsCount = requests.Count(x => x.OpportunityType == "توظيف"),
            TrainingCount = requests.Count(x => x.OpportunityType == "تدريب تعاوني"),
            VolunteerCount = requests.Count(x => x.OpportunityType == "تطوع"),
            TotalAvailableOpportunities = totalAvailableOpportunities,

            CandidatesCount = candidates.Count,
            NewCandidatesCount = candidates.Count(x =>
                x.Status == "مرشح جديد"),
            InterviewCandidatesCount = candidates.Count(x =>
                x.Status == "تمت المقابلة"),
            AcceptedCandidatesCount = acceptedCandidatesCount,
            RejectedCandidatesCount = candidates.Count(x =>
                x.Status == "لم يتم القبول"),
            CandidatesThisMonth = candidates.Count(x =>
                x.CreatedAt >= monthStart),

            SuccessRate = candidates.Count == 0
                ? 0
                : Math.Round(
                    acceptedCandidatesCount * 100d / candidates.Count,
                    1),

            FulfillmentRate = totalAvailableOpportunities == 0
                ? 0
                : Math.Round(
                    Math.Min(
                        100,
                        acceptedCandidatesCount * 100d /
                        totalAvailableOpportunities),
                    1),

            AverageCandidatesPerRequest = requestsCount == 0
                ? 0
                : Math.Round(
                    candidates.Count * 1d / requestsCount,
                    1),

            ProfileCompletion = (int)Math.Round(
                profileFields.Count(x => x) * 100d /
                profileFields.Length),

            UnreadNotificationsCount = await notificationsQuery
                .CountAsync(x => !x.IsRead),

            RequestStatusCounts = RequestStages.ToDictionary(
                status => status,
                status => requests.Count(x => x.Status == status)),

            LatestRequests = requests
                .Take(6)
                .ToList(),

            LatestNotifications = await notificationsQuery
                .OrderByDescending(x => x.CreatedAt)
                .Take(6)
                .ToListAsync()
        };

        return View("~/Areas/Portal/Views/OrganizationDashboard/Index.cshtml", model);
    }

    private int GetOrganizationId()
    {
        var value = User.FindFirstValue("KafoOrganizationUserId");
        return int.TryParse(value, out var organizationId)
            ? organizationId
            : 0;
    }
}
