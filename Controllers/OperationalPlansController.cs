using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[Route("OperationalPlans")]
[Route("Governance/OperationalPlans")]
public class OperationalPlansController : Controller
{
    private readonly ApplicationDbContext _context;

    public OperationalPlansController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var documents = await _context.OperationalPlanDocuments
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync();

        return View("~/Views/OperationalPlans/Index.cshtml", documents);
    }
}
