using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.ViewModels.Donor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
public class DonorReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    public DonorReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Portal/Donor/Reports")]
    public async Task<IActionResult> Index(int? year, string? status)
    {
        var donorId = GetDonorId();
        if (donorId <= 0) return Forbid();

        var baseQuery = _context.DonorContributions
            .AsNoTracking()
            .Include(x => x.ProgramProject)
            .Include(x => x.Reports)
            .Where(x => x.DonorAccountId == donorId);

        var availableYears = await baseQuery
            .Select(x => x.CreatedAt.Year)
            .Distinct()
            .OrderByDescending(x => x)
            .ToListAsync();

        if (year.HasValue)
            baseQuery = baseQuery.Where(x => x.CreatedAt.Year == year.Value);

        if (!string.IsNullOrWhiteSpace(status))
            baseQuery = baseQuery.Where(x => x.Status == status);

        var contributions = await baseQuery
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var model = new DonorReportsIndexViewModel
        {
            Contributions = contributions.Select(x => new DonorContributionReportItemViewModel
            {
                Contribution = x,
                Reports = x.Reports.OrderByDescending(r => r.ReportDate).ToList()
            }).ToList(),
            TotalContributions = contributions.Count,
            TotalSupportAmount = contributions.Sum(x => x.TotalAmount),
            TotalSpentAmount = contributions.Sum(x => x.SpentAmount),
            TotalBeneficiaries = contributions.Sum(x => x.BeneficiariesCount),
            DownloadableFiles = contributions.Sum(x => x.Reports.Count(r => !string.IsNullOrWhiteSpace(r.FilePath))),
            SelectedYear = year,
            SelectedStatus = status,
            AvailableYears = availableYears
        };

        return View("~/Areas/Portal/Views/DonorReports/Index.cshtml", model);
    }

    [HttpGet("/Portal/Donor/Reports/Print/{id:int}")]
    public async Task<IActionResult> Print(int id)
    {
        var donorId = GetDonorId();
        var donor = await _context.DonorAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == donorId);
        var contribution = await _context.DonorContributions
            .AsNoTracking()
            .Include(x => x.ProgramProject)
            .Include(x => x.Updates)
            .Include(x => x.Reports)
            .FirstOrDefaultAsync(x => x.Id == id && x.DonorAccountId == donorId);

        if (contribution is null || donor is null) return NotFound();

        var model = new DonorSupportPrintViewModel
        {
            DonorName = donor.FullName,
            DonorCode = donor.Email,
            ReportTitle = $"تقرير دعم: {contribution.Title}",
            Contributions = new() { contribution }
        };

        return View("~/Areas/Portal/Views/DonorReports/Print.cshtml", model);
    }

    [HttpGet("/Portal/Donor/Reports/PrintAll")]
    public async Task<IActionResult> PrintAll(int? year, string? status)
    {
        var donorId = GetDonorId();
        var donor = await _context.DonorAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == donorId);
        if (donor is null) return NotFound();

        var query = _context.DonorContributions
            .AsNoTracking()
            .Include(x => x.ProgramProject)
            .Include(x => x.Updates)
            .Include(x => x.Reports)
            .Where(x => x.DonorAccountId == donorId);

        if (year.HasValue) query = query.Where(x => x.CreatedAt.Year == year.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);

        var contributions = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        var model = new DonorSupportPrintViewModel
        {
            DonorName = donor.FullName,
            DonorCode = donor.Email,
            ReportTitle = year.HasValue ? $"تقرير الدعم الشامل لعام {year}" : "تقرير الدعم الشامل",
            Contributions = contributions
        };

        return View("~/Areas/Portal/Views/DonorReports/Print.cshtml", model);
    }

    [HttpGet("/Portal/Donor/Reports/File/{id:int}")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var donorId = GetDonorId();
        var report = await _context.DonorReports
            .AsNoTracking()
            .Include(x => x.DonorContribution)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.DonorContribution != null &&
                x.DonorContribution.DonorAccountId == donorId);

        if (report is null || string.IsNullOrWhiteSpace(report.FilePath))
            return NotFound();

        return Redirect(report.FilePath);
    }

    private int GetDonorId()
    {
        var value = User.FindFirstValue("KafoDonorUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var donorId) ? donorId : 0;
    }

}
