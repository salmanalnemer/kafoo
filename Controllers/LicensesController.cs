using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

public class LicensesController : Controller
{
    private readonly ApplicationDbContext _context;

    public LicensesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var licenses = await _context.LicenseDocuments
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(licenses);
    }

    public async Task<IActionResult> Details(int id)
    {
        var license = await _context.LicenseDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        if (license == null)
            return NotFound();

        return View(license);
    }
}
