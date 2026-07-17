using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

public class ServiceLinksController : Controller
{
    private readonly ApplicationDbContext _context;

    public ServiceLinksController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? category)
    {
        var query = _context.ServiceLinks
            .AsNoTracking()
            .Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(x => x.Category == category);

        var links = await query
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        ViewBag.SelectedCategory = category;
        ViewBag.Categories = await _context.ServiceLinks
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.Category)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return View(links);
    }

    [HttpGet("/Services")]
    public Task<IActionResult> Services(string? category)
    {
        return Index(category);
    }
}
