using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class ProgramsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILogger<ProgramsController> _logger;

    public ProgramsController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILogger<ProgramsController> logger)
    {
        _context = context;
        _files = files;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _context.ProgramProjects
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(items);
    }

    public IActionResult Create()
    {
        return View(new ProgramProject
        {
            IsActive = true,
            Category = "برنامج",
            DisplayOrder = 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProgramProject model, IFormFile? imageFile)
    {
        ModelState.Remove(nameof(ProgramProject.ImagePath));
        ModelState.Remove(nameof(ProgramProject.CreatedAt));
        ModelState.Remove(nameof(ProgramProject.UpdatedAt));

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            if (imageFile != null && imageFile.Length > 0)
                model.ImagePath = await _files.UploadAsync(imageFile, "programs");

            NormalizeProgram(model);

            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            _context.ProgramProjects.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة البرنامج أو المشروع بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating program project");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.ProgramProjects.FindAsync(id);

        if (item == null)
            return NotFound();

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProgramProject model, IFormFile? imageFile)
    {
        if (id != model.Id)
            return BadRequest();

        ModelState.Remove(nameof(ProgramProject.CreatedAt));
        ModelState.Remove(nameof(ProgramProject.UpdatedAt));

        if (!ModelState.IsValid)
            return View(model);

        var item = await _context.ProgramProjects.FindAsync(id);

        if (item == null)
            return NotFound();

        try
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                _files.Delete(item.ImagePath);
                item.ImagePath = await _files.UploadAsync(imageFile, "programs");
            }

            item.Title = model.Title.Trim();
            item.Description = model.Description.Trim();
            item.Details = string.IsNullOrWhiteSpace(model.Details) ? null : model.Details.Trim();
            item.Category = string.IsNullOrWhiteSpace(model.Category) ? "برنامج" : model.Category.Trim();
            item.Beneficiaries = string.IsNullOrWhiteSpace(model.Beneficiaries) ? null : model.Beneficiaries.Trim();
            item.BeneficiariesCount = string.IsNullOrWhiteSpace(model.BeneficiariesCount) ? null : model.BeneficiariesCount.Trim();
            item.Sector = string.IsNullOrWhiteSpace(model.Sector) ? null : model.Sector.Trim();
            item.ExternalUrl = string.IsNullOrWhiteSpace(model.ExternalUrl) ? null : model.ExternalUrl.Trim();
            item.DisplayOrder = model.DisplayOrder;
            item.IsActive = model.IsActive;
            item.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تعديل البرنامج أو المشروع بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while editing program project {ProgramProjectId}", id);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var item = await _context.ProgramProjects.FindAsync(id);

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
        var item = await _context.ProgramProjects.FindAsync(id);

        if (item == null)
            return NotFound();

        _files.Delete(item.ImagePath);
        _context.ProgramProjects.Remove(item);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف البرنامج أو المشروع بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    private static void NormalizeProgram(ProgramProject model)
    {
        model.Title = model.Title.Trim();
        model.Description = model.Description.Trim();
        model.Details = string.IsNullOrWhiteSpace(model.Details) ? null : model.Details.Trim();
        model.Category = string.IsNullOrWhiteSpace(model.Category) ? "برنامج" : model.Category.Trim();
        model.Beneficiaries = string.IsNullOrWhiteSpace(model.Beneficiaries) ? null : model.Beneficiaries.Trim();
        model.BeneficiariesCount = string.IsNullOrWhiteSpace(model.BeneficiariesCount) ? null : model.BeneficiariesCount.Trim();
        model.Sector = string.IsNullOrWhiteSpace(model.Sector) ? null : model.Sector.Trim();
        model.ExternalUrl = string.IsNullOrWhiteSpace(model.ExternalUrl) ? null : model.ExternalUrl.Trim();
    }
}
