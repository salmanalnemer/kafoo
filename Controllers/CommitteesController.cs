using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[Route("Committees")]
[Route("Governance/Committees")]
public class CommitteesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CommitteesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var documents = await _context.CommitteeDocuments
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.Year)
            .ThenBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        return View("~/Views/Committees/Index.cshtml", documents);
    }
}
