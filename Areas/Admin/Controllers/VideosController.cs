using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/Videos")]
public class VideosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public VideosController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q)
    {
        var query = _context.VideoLibraryItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(x =>
                x.Title.Contains(q) ||
                x.Category.Contains(q) ||
                (x.Description != null && x.Description.Contains(q)));
        }

        var videos = await query
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        ViewBag.Search = q ?? "";

        return View("~/Areas/Admin/Views/Videos/Index.cshtml", videos);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        var model = new VideoLibraryItem
        {
            Category = "مكتبة الفيديو",
            VideoSourceType = "Youtube",
            PublishedAt = DateTime.Now,
            IsActive = true
        };

        return View("~/Areas/Admin/Views/Videos/Create.cshtml", model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(150_000_000)]
    public async Task<IActionResult> Create(VideoLibraryItem model, IFormFile? videoFile, IFormFile? thumbnailFile)
    {
        ModelState.Remove(nameof(model.VideoPath));
        ModelState.Remove(nameof(model.ThumbnailPath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        ValidateVideoSource(model, videoFile);

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/Videos/Create.cshtml", model);

        if (thumbnailFile != null && thumbnailFile.Length > 0)
            model.ThumbnailPath = await _files.UploadAsync(thumbnailFile, "video-thumbnails");

        if (model.VideoSourceType == "Upload" && videoFile != null && videoFile.Length > 0)
            model.VideoPath = await _files.UploadAsync(videoFile, "video-library");

        model.Title = model.Title.Trim();
        model.Category = string.IsNullOrWhiteSpace(model.Category) ? "مكتبة الفيديو" : model.Category.Trim();
        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        model.YoutubeUrl = string.IsNullOrWhiteSpace(model.YoutubeUrl) ? null : model.YoutubeUrl.Trim();
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;

        _context.VideoLibraryItems.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إضافة الفيديو بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var video = await _context.VideoLibraryItems.FindAsync(id);

        if (video == null)
            return NotFound();

        return View("~/Areas/Admin/Views/Videos/Edit.cshtml", video);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(150_000_000)]
    public async Task<IActionResult> Edit(int id, VideoLibraryItem model, IFormFile? videoFile, IFormFile? thumbnailFile)
    {
        if (id != model.Id)
            return BadRequest();

        ModelState.Remove(nameof(model.VideoPath));
        ModelState.Remove(nameof(model.ThumbnailPath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        var video = await _context.VideoLibraryItems.FindAsync(id);

        if (video == null)
            return NotFound();

        ValidateVideoSource(model, videoFile, video.VideoPath);

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/Videos/Edit.cshtml", model);

        if (thumbnailFile != null && thumbnailFile.Length > 0)
            video.ThumbnailPath = await _files.UploadAsync(thumbnailFile, "video-thumbnails");

        if (model.VideoSourceType == "Upload" && videoFile != null && videoFile.Length > 0)
            video.VideoPath = await _files.UploadAsync(videoFile, "video-library");

        video.Title = model.Title.Trim();
        video.Category = string.IsNullOrWhiteSpace(model.Category) ? "مكتبة الفيديو" : model.Category.Trim();
        video.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        video.VideoSourceType = string.IsNullOrWhiteSpace(model.VideoSourceType) ? "Youtube" : model.VideoSourceType;
        video.YoutubeUrl = string.IsNullOrWhiteSpace(model.YoutubeUrl) ? null : model.YoutubeUrl.Trim();
        video.PublishedAt = model.PublishedAt;
        video.IsFeatured = model.IsFeatured;
        video.IsActive = model.IsActive;
        video.DisplayOrder = model.DisplayOrder;
        video.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تحديث الفيديو بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Toggle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var video = await _context.VideoLibraryItems.FindAsync(id);

        if (video == null)
            return NotFound();

        video.IsActive = !video.IsActive;
        video.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var video = await _context.VideoLibraryItems.FindAsync(id);

        if (video == null)
            return NotFound();

        _context.VideoLibraryItems.Remove(video);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف الفيديو بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateVideoSource(VideoLibraryItem model, IFormFile? videoFile, string? existingVideoPath = null)
    {
        model.VideoSourceType = string.IsNullOrWhiteSpace(model.VideoSourceType) ? "Youtube" : model.VideoSourceType;

        if (model.VideoSourceType == "Youtube" && string.IsNullOrWhiteSpace(model.YoutubeUrl))
            ModelState.AddModelError(nameof(model.YoutubeUrl), "رابط YouTube مطلوب.");

        if (model.VideoSourceType == "Upload" && videoFile == null && string.IsNullOrWhiteSpace(existingVideoPath))
            ModelState.AddModelError(nameof(model.VideoPath), "رفع ملف الفيديو مطلوب.");
    }
}
