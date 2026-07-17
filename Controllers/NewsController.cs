using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

public class NewsController : Controller
{
    private readonly ApplicationDbContext _context;

    public NewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var news = await _context.NewsPosts
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.PublishDate)
            .ToListAsync();

        return View(news);
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _context.NewsPosts
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        if (item == null)
            return NotFound();

        return View(item);
    }
}
