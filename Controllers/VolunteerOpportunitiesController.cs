using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[Route("VolunteerOpportunities")]
public class VolunteerOpportunitiesController : Controller
{
    private readonly ApplicationDbContext _context;

    public VolunteerOpportunitiesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var opportunities = await _context.VolunteerOpportunities
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        return View("~/Views/VolunteerOpportunities/Index.cshtml", opportunities);
    }
}
