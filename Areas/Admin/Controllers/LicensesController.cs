using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class LicensesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILogger<LicensesController> _logger;

    public LicensesController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILogger<LicensesController> logger)
    {
        _context = context;
        _files = files;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _context.LicenseDocuments
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(items);
    }

    public IActionResult Create()
    {
        return View(new LicenseDocument
        {
            IsActive = true,
            DisplayOrder = 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LicenseDocument model, IFormFile? licenseFile)
    {
        ModelState.Remove(nameof(LicenseDocument.FilePath));
        ModelState.Remove(nameof(LicenseDocument.FileType));
        ModelState.Remove(nameof(LicenseDocument.CreatedAt));
        ModelState.Remove(nameof(LicenseDocument.UpdatedAt));

        if (licenseFile == null || licenseFile.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "يجب اختيار صورة أو ملف PDF للترخيص.");
        }

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var uploadedPath = await _files.UploadAsync(licenseFile!, "licenses");

            model.FilePath = uploadedPath;
            model.FileType = uploadedPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "pdf" : "image";
            model.Title = model.Title.Trim();
            model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            _context.LicenseDocuments.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة الترخيص بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating license");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.LicenseDocuments.FindAsync(id);

        if (item == null)
            return NotFound();

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LicenseDocument model, IFormFile? licenseFile)
    {
        if (id != model.Id)
            return BadRequest();

        ModelState.Remove(nameof(LicenseDocument.CreatedAt));
        ModelState.Remove(nameof(LicenseDocument.UpdatedAt));

        if (!ModelState.IsValid)
            return View(model);

        var item = await _context.LicenseDocuments.FindAsync(id);

        if (item == null)
            return NotFound();

        try
        {
            if (licenseFile != null && licenseFile.Length > 0)
            {
                _files.Delete(item.FilePath);

                var uploadedPath = await _files.UploadAsync(licenseFile, "licenses");
                item.FilePath = uploadedPath;
                item.FileType = uploadedPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "pdf" : "image";
            }

            item.Title = model.Title.Trim();
            item.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            item.DisplayOrder = model.DisplayOrder;
            item.IsActive = model.IsActive;
            item.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تعديل الترخيص بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while editing license {LicenseId}", id);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var item = await _context.LicenseDocuments.FindAsync(id);

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
        var item = await _context.LicenseDocuments.FindAsync(id);

        if (item == null)
            return NotFound();

        _files.Delete(item.FilePath);
        _context.LicenseDocuments.Remove(item);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف الترخيص بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
