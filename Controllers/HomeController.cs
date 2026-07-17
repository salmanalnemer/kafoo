using Kafo.Web.Data;
using Kafo.Web.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        DateTime now = DateTime.Now;

        var model = new HomeIndexViewModel
        {
            Sliders = await _context.Sliders
                .Where(x => x.IsActive)
                .Where(x => x.PublishStart == null || x.PublishStart <= now)
                .Where(x => x.PublishEnd == null || x.PublishEnd >= now)
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync(),

            Statistics = await _context.HomeStatistics
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync(),

            StrategicGoals = await _context.StrategicGoals
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync(),

            Programs = await _context.ProgramProjects
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.CreatedAt)
                .Take(4)
                .ToListAsync(),

            News = await _context.NewsPosts
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.PublishDate)
                .Take(4)
                .ToListAsync(),

            Partners = await _context.SuccessPartners
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync()
        };

        return View(model);
    }

    public IActionResult Error()
    {
        Response.StatusCode = 500;
        return View();
    }
}
