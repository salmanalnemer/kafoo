using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[Route("SuccessPartners")]
public class SuccessPartnersController : Controller
{
    private readonly ApplicationDbContext _context;

    public SuccessPartnersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var partners = await _context.SuccessPartners
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        return View("~/Views/SuccessPartners/Index.cshtml", partners);
    }
}
