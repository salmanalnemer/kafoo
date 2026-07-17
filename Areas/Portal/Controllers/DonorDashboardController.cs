using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.ViewModels.Donor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
public class DonorDashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DonorDashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Portal/Donor")]
    [HttpGet("/Portal/Donor/Dashboard")]
    public async Task<IActionResult> Index()
    {
        var donorId = GetDonorId();

        var contributionsQuery = _context.DonorContributions
            .AsNoTracking()
            .Include(x => x.ProgramProject)
            .Where(x => x.DonorAccountId == donorId);

        var contributions = await contributionsQuery
            .OrderByDescending(x => x.CreatedAt)
            .Take(6)
            .ToListAsync();

        var contributionIds = await contributionsQuery.Select(x => x.Id).ToListAsync();

        var totalAmount = await contributionsQuery.SumAsync(x => (decimal?)x.TotalAmount) ?? 0;
        var totalSpent = await contributionsQuery.SumAsync(x => (decimal?)x.SpentAmount) ?? 0;
        var totalRemaining = await contributionsQuery.SumAsync(x => (decimal?)x.RemainingAmount) ?? 0;
        var contributionsCount = await contributionsQuery.CountAsync();
        var beneficiariesCount = await contributionsQuery.SumAsync(x => (int?)x.BeneficiariesCount) ?? 0;
        var averageProgress = await contributionsQuery.AnyAsync()
            ? await contributionsQuery.AverageAsync(x => x.ProgressPercent)
            : 0;

        var model = new DonorDashboardViewModel
        {
            TotalAmount = totalAmount,
            TotalSpent = totalSpent,
            TotalRemaining = totalRemaining,
            ContributionsCount = contributionsCount,
            ActiveContributionsCount = await contributionsQuery.CountAsync(x => x.Status == "قيد التنفيذ" || x.Status == "مستمر" || x.Status == "بانتظار الاعتماد"),
            CompletedContributionsCount = await contributionsQuery.CountAsync(x => x.Status == "مكتمل"),
            ProgramsCount = await contributionsQuery
                .Where(x => x.ProgramProjectId != null)
                .Select(x => x.ProgramProjectId)
                .Distinct()
                .CountAsync(),
            BeneficiariesCount = beneficiariesCount,
            AverageProgress = averageProgress,
            ReportsCount = await _context.DonorReports.CountAsync(x => contributionIds.Contains(x.DonorContributionId)),
            PendingSurplusCount = await contributionsQuery.CountAsync(x => x.HasSurplus && x.RemainingAmount > 0 && x.IsSurplusLocked),
            UnreadNotificationsCount = await _context.DonorNotifications.CountAsync(x => x.DonorAccountId == donorId && !x.IsRead),
            LatestContributions = contributions,
            LatestNotifications = await _context.DonorNotifications
                .AsNoTracking()
                .Where(x => x.DonorAccountId == donorId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync(),
            LatestReports = await _context.DonorReports
                .AsNoTracking()
                .Include(x => x.DonorContribution)
                .Where(x => contributionIds.Contains(x.DonorContributionId))
                .OrderByDescending(x => x.ReportDate)
                .Take(5)
                .ToListAsync()
        };

        return View("~/Areas/Portal/Views/DonorDashboard/Index.cshtml", model);
    }

    private int GetDonorId()
    {
        var value = User.FindFirstValue("KafoDonorUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var donorId) ? donorId : 0;
    }
}
