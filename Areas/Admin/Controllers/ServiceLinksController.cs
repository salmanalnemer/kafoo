using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class ServiceLinksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILogger<ServiceLinksController> _logger;

    public ServiceLinksController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILogger<ServiceLinksController> logger)
    {
        _context = context;
        _files = files;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _context.ServiceLinks
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(items);
    }

    public IActionResult Create()
    {
        return View(new ServiceLink
        {
            IsActive = true,
            OpenInNewTab = true,
            Category = "خدمة إلكترونية",
            ButtonText = "الدخول للخدمة",
            DisplayOrder = 0,
            LinkType = "External"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceLink model, IFormFile? imageFile, string? externalUrl)
    {
        CleanModelState();

        ApplyLinkTarget(model, externalUrl);

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            if (imageFile != null && imageFile.Length > 0)
                model.ImagePath = await _files.UploadAsync(imageFile, "service-links");

            NormalizeServiceLink(model);

            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            _context.ServiceLinks.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة رابط الخدمة بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating service link");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.ServiceLinks.FindAsync(id);

        if (item == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(item.LinkType))
            item.LinkType = item.Url.StartsWith("/") ? "Internal" : "External";

        if (item.LinkType == "Internal" && string.IsNullOrWhiteSpace(item.InternalPath))
            item.InternalPath = item.Url;

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceLink model, IFormFile? imageFile, string? externalUrl)
    {
        if (id != model.Id)
            return BadRequest();

        CleanModelState();

        ApplyLinkTarget(model, externalUrl);

        if (!ModelState.IsValid)
            return View(model);

        var item = await _context.ServiceLinks.FindAsync(id);

        if (item == null)
            return NotFound();

        try
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                _files.Delete(item.ImagePath);
                item.ImagePath = await _files.UploadAsync(imageFile, "service-links");
            }

            item.Title = model.Title.Trim();
            item.Description = model.Description.Trim();
            item.Url = model.Url;
            item.LinkType = model.LinkType;
            item.InternalPath = model.LinkType == "Internal" ? model.InternalPath : null;
            item.Category = string.IsNullOrWhiteSpace(model.Category) ? "خدمة إلكترونية" : model.Category.Trim();
            item.TargetAudience = string.IsNullOrWhiteSpace(model.TargetAudience) ? null : model.TargetAudience.Trim();
            item.ButtonText = string.IsNullOrWhiteSpace(model.ButtonText) ? "الدخول للخدمة" : model.ButtonText.Trim();
            item.OpenInNewTab = model.LinkType == "External" && model.OpenInNewTab;
            item.DisplayOrder = model.DisplayOrder;
            item.IsActive = model.IsActive;
            item.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تعديل رابط الخدمة بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while editing service link {ServiceLinkId}", id);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var item = await _context.ServiceLinks.FindAsync(id);

        if (item == null)
            return NotFound();

        item.IsActive = !item.IsActive;
        item.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.ServiceLinks.FindAsync(id);

        if (item == null)
            return NotFound();

        _files.Delete(item.ImagePath);
        _context.ServiceLinks.Remove(item);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف رابط الخدمة بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    private void CleanModelState()
    {
        ModelState.Remove(nameof(ServiceLink.Url));
        ModelState.Remove(nameof(ServiceLink.ImagePath));
        ModelState.Remove(nameof(ServiceLink.CreatedAt));
        ModelState.Remove(nameof(ServiceLink.UpdatedAt));
    }

    private void ApplyLinkTarget(ServiceLink model, string? externalUrl)
    {
        model.LinkType = string.IsNullOrWhiteSpace(model.LinkType) ? "External" : model.LinkType.Trim();

        if (model.LinkType == "Internal")
        {
            if (string.IsNullOrWhiteSpace(model.InternalPath))
            {
                ModelState.AddModelError(string.Empty, "اختر الصفحة الداخلية.");
                return;
            }

            model.Url = model.InternalPath.Trim();
            model.OpenInNewTab = false;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(externalUrl))
            {
                ModelState.AddModelError(string.Empty, "اكتب الرابط الخارجي.");
                return;
            }

            model.Url = NormalizeUrl(externalUrl);
            model.InternalPath = null;
        }
    }

    private static void NormalizeServiceLink(ServiceLink model)
    {
        model.Title = model.Title.Trim();
        model.Description = model.Description.Trim();
        model.Category = string.IsNullOrWhiteSpace(model.Category) ? "خدمة إلكترونية" : model.Category.Trim();
        model.TargetAudience = string.IsNullOrWhiteSpace(model.TargetAudience) ? null : model.TargetAudience.Trim();
        model.ButtonText = string.IsNullOrWhiteSpace(model.ButtonText) ? "الدخول للخدمة" : model.ButtonText.Trim();
    }

    private static string NormalizeUrl(string value)
    {
        var url = value.Trim();

        if (url.StartsWith("/") ||
            url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
            return url;

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return "https://" + url;

        return url;
    }
}
