using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[Route("GeneralAssemblyMinutes")]
[Route("Governance/GeneralAssemblyMinutes")]
public class GeneralAssemblyMinutesController : Controller
{
    private readonly ApplicationDbContext _context;

    public GeneralAssemblyMinutesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var minutes = await _context.GeneralAssemblyMinutes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.MeetingDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        return View("~/Views/GeneralAssemblyMinutes/Index.cshtml", minutes);
    }
}
