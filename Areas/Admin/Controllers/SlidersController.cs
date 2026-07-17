using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class SlidersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<SlidersController> _logger;

    public SlidersController(
        ApplicationDbContext context,
        IFileUploadService fileUploadService,
        ILogger<SlidersController> logger)
    {
        _context = context;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var sliders = await _context.Sliders
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(sliders);
    }

    public IActionResult Create()
    {
        return View(new SliderFormViewModel
        {
            IsActive = true,
            DisplayOrder = 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SliderFormViewModel model)
    {
        if (model.ImageFile == null)
            ModelState.AddModelError(nameof(model.ImageFile), "صورة السلايدر مطلوبة.");

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            string imagePath = await _fileUploadService.UploadAsync(model.ImageFile!, "sliders");

            var slider = new Slider
            {
                Title = string.IsNullOrWhiteSpace(model.Title) ? null : model.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                ButtonText = string.IsNullOrWhiteSpace(model.ButtonText) ? null : model.ButtonText.Trim(),
                ButtonUrl = string.IsNullOrWhiteSpace(model.ButtonUrl) ? null : model.ButtonUrl.Trim(),
                ImagePath = imagePath,
                DisplayOrder = model.DisplayOrder,
                IsActive = model.IsActive,
                PublishStart = model.PublishStart,
                PublishEnd = model.PublishEnd,
                CreatedAt = DateTime.Now
            };

            _context.Sliders.Add(slider);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة السلايدر بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating slider");
            ModelState.AddModelError(string.Empty, "حدث خطأ أثناء حفظ السلايدر.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var slider = await _context.Sliders.FindAsync(id);

        if (slider == null)
            return NotFound();

        var model = new SliderFormViewModel
        {
            Id = slider.Id,
            Title = slider.Title,
            Description = slider.Description,
            ButtonText = slider.ButtonText,
            ButtonUrl = slider.ButtonUrl,
            DisplayOrder = slider.DisplayOrder,
            IsActive = slider.IsActive,
            PublishStart = slider.PublishStart,
            PublishEnd = slider.PublishEnd,
            CurrentImagePath = slider.ImagePath
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SliderFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var slider = await _context.Sliders.FindAsync(id);

        if (slider == null)
            return NotFound();

        try
        {
            if (model.ImageFile != null)
            {
                string oldImagePath = slider.ImagePath;
                string newImagePath = await _fileUploadService.UploadAsync(model.ImageFile, "sliders");

                slider.ImagePath = newImagePath;
                _fileUploadService.Delete(oldImagePath);
            }

            slider.Title = string.IsNullOrWhiteSpace(model.Title) ? null : model.Title.Trim();
            slider.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            slider.ButtonText = string.IsNullOrWhiteSpace(model.ButtonText) ? null : model.ButtonText.Trim();
            slider.ButtonUrl = string.IsNullOrWhiteSpace(model.ButtonUrl) ? null : model.ButtonUrl.Trim();
            slider.DisplayOrder = model.DisplayOrder;
            slider.IsActive = model.IsActive;
            slider.PublishStart = model.PublishStart;
            slider.PublishEnd = model.PublishEnd;
            slider.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تعديل السلايدر بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while editing slider {SliderId}", id);
            ModelState.AddModelError(string.Empty, "حدث خطأ أثناء تعديل السلايدر.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var slider = await _context.Sliders.FindAsync(id);

        if (slider == null)
            return NotFound();

        slider.IsActive = !slider.IsActive;
        slider.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var slider = await _context.Sliders.FindAsync(id);

        if (slider == null)
            return NotFound();

        _fileUploadService.Delete(slider.ImagePath);
        _context.Sliders.Remove(slider);

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف السلايدر بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
