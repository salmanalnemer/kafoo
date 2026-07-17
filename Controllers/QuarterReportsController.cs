using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[Route("QuarterReports")]
[Route("Governance/QuarterReports")]
public class QuarterReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public QuarterReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var documents = await _context.QuarterReportDocuments
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync();

        return View("~/Views/QuarterReports/Index.cshtml", documents);
    }
}
