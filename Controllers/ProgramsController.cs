using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

public class ProgramsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProgramsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? category)
    {
        var query = _context.ProgramProjects
            .AsNoTracking()
            .Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(x => x.Category == category);

        var programs = await query
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        ViewBag.SelectedCategory = category;
        ViewBag.Categories = await _context.ProgramProjects
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.Category)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return View(programs);
    }

    public async Task<IActionResult> Details(int id)
    {
        var program = await _context.ProgramProjects
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        if (program == null)
            return NotFound();

        return View(program);
    }
}
