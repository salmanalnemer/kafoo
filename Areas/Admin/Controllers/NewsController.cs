using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class NewsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public NewsController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _context.NewsPosts
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.PublishDate)
            .ToListAsync();

        return View(items);
    }

    public IActionResult Create()
    {
        return View(new NewsPost
        {
            IsActive = true,
            PublishDate = DateTime.Now
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NewsPost model, IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (imageFile != null)
            model.ImagePath = await _files.UploadAsync(imageFile, "news");

        model.CreatedAt = DateTime.Now;
        _context.NewsPosts.Add(model);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.NewsPosts.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NewsPost model, IFormFile? imageFile)
    {
        if (id != model.Id)
            return BadRequest();

        var item = await _context.NewsPosts.FindAsync(id);
        if (item == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        if (imageFile != null)
        {
            _files.Delete(item.ImagePath);
            item.ImagePath = await _files.UploadAsync(imageFile, "news");
        }

        item.Title = model.Title;
        item.Summary = model.Summary;
        item.Details = model.Details;
        item.Category = model.Category;
        item.Audience = model.Audience;
        item.PublishDate = model.PublishDate;
        item.DisplayOrder = model.DisplayOrder;
        item.IsActive = model.IsActive;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.NewsPosts.FindAsync(id);
        if (item == null)
            return NotFound();

        _files.Delete(item.ImagePath);
        _context.NewsPosts.Remove(item);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
