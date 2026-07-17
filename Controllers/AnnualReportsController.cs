using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[Route("AnnualReports")]
[Route("Governance/AnnualReports")]
public class AnnualReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AnnualReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var documents = await _context.AnnualReportDocuments
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync();

        return View("~/Views/AnnualReports/Index.cshtml", documents);
    }
}
