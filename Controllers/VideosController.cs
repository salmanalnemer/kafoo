using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[Route("Videos")]
public class VideosController : Controller
{
    private readonly ApplicationDbContext _context;

    public VideosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var videos = await _context.VideoLibraryItems
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        return View("~/Views/Videos/Index.cshtml", videos);
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var video = await _context.VideoLibraryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        if (video == null)
            return NotFound();

        var latest = await _context.VideoLibraryItems
            .AsNoTracking()
            .Where(x => x.IsActive && x.Id != id)
            .OrderByDescending(x => x.PublishedAt)
            .Take(3)
            .ToListAsync();

        ViewBag.LatestVideos = latest;

        return View("~/Views/Videos/Details.cshtml", video);
    }
}
