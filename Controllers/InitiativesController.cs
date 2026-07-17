using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[Route("Initiatives")]
public class InitiativesController : Controller
{
    private readonly ApplicationDbContext _context;

    public InitiativesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var initiatives = await _context.Initiatives
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        return View("~/Views/Initiatives/Index.cshtml", initiatives);
    }
}
