using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.ViewModels.Donor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Donor.Controllers;

[Area("Donor")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

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
            ProgramsCount = await contributionsQuery
                .Where(x => x.ProgramProjectId != null)
                .Select(x => x.ProgramProjectId)
                .Distinct()
                .CountAsync(),
            BeneficiariesCount = beneficiariesCount,
            AverageProgress = averageProgress,
            UnreadNotificationsCount = await _context.DonorNotifications.CountAsync(x => x.DonorAccountId == donorId && !x.IsRead),
            LatestContributions = contributions,
            LatestNotifications = await _context.DonorNotifications
                .AsNoTracking()
                .Where(x => x.DonorAccountId == donorId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync()
        };

        return View(model);
    }

    private int GetDonorId()
    {
        var value = User.FindFirstValue("KafoDonorUserId");
        return int.TryParse(value, out var donorId) ? donorId : 0;
    }
}
