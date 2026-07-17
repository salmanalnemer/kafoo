using Kafo.Web.Data;
using Kafo.Web.ViewModels.Donor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
public class DonorDisclosureController : Controller
{
    private readonly ApplicationDbContext _context;

    public DonorDisclosureController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Portal/Donor/Disclosure")]
    public async Task<IActionResult> Index()
    {
        var model = new DonorDisclosureViewModel
        {
            AnnualReports = await _context.AnnualReportDocuments.AsNoTracking().Where(x => x.IsActive).OrderByDescending(x => x.Year).ThenBy(x => x.DisplayOrder).Take(8).ToListAsync(),
            QuarterReports = await _context.QuarterReportDocuments.AsNoTracking().Where(x => x.IsActive).OrderByDescending(x => x.Year).ThenBy(x => x.DisplayOrder).Take(8).ToListAsync(),
            FinancialStatements = await _context.FinancialStatementDocuments.AsNoTracking().Where(x => x.IsActive).OrderByDescending(x => x.Year).ThenBy(x => x.DisplayOrder).Take(8).ToListAsync(),
            OperationalPlans = await _context.OperationalPlanDocuments.AsNoTracking().Where(x => x.IsActive).OrderByDescending(x => x.Year).ThenBy(x => x.DisplayOrder).Take(8).ToListAsync(),
            Policies = await _context.PolicyDocuments.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).Take(8).ToListAsync(),
            AidReports = await _context.AidReports.AsNoTracking().Where(x => x.IsActive).OrderByDescending(x => x.PublishedAt).Take(8).ToListAsync()
        };

        return View("~/Areas/Portal/Views/DonorDisclosure/Index.cshtml", model);
    }
}
