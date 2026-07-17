using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[Route("AidReports")]
[Route("AssistanceReports")]
public class AidReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AidReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var reports = await _context.AidReports
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        return View("~/Views/AidReports/Index.cshtml", reports);
    }
}
