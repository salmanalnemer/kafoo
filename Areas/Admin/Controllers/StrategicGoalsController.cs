using Kafo.Web.Data;
using Kafo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class StrategicGoalsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StrategicGoalsController> _logger;

    public StrategicGoalsController(
        ApplicationDbContext context,
        ILogger<StrategicGoalsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _context.StrategicGoals
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        return View(items);
    }

    public IActionResult Create()
    {
        return View(new StrategicGoal
        {
            IsActive = true,
            DisplayOrder = 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StrategicGoal model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            model.Title = model.Title.Trim();
            model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            model.Icon = string.IsNullOrWhiteSpace(model.Icon) ? null : model.Icon.Trim();
            model.CreatedAt = DateTime.Now;

            _context.StrategicGoals.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة الهدف بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating strategic goal");
            ModelState.AddModelError(string.Empty, "حدث خطأ أثناء حفظ الهدف.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.StrategicGoals.FindAsync(id);

        if (item == null)
            return NotFound();

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StrategicGoal model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var item = await _context.StrategicGoals.FindAsync(id);

        if (item == null)
            return NotFound();

        try
        {
            item.Title = model.Title.Trim();
            item.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            item.Icon = string.IsNullOrWhiteSpace(model.Icon) ? null : model.Icon.Trim();
            item.DisplayOrder = model.DisplayOrder;
            item.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تعديل الهدف بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while editing strategic goal {GoalId}", id);
            ModelState.AddModelError(string.Empty, "حدث خطأ أثناء تعديل الهدف.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var item = await _context.StrategicGoals.FindAsync(id);

        if (item == null)
            return NotFound();

        item.IsActive = !item.IsActive;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.StrategicGoals.FindAsync(id);

        if (item == null)
            return NotFound();

        _context.StrategicGoals.Remove(item);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف الهدف بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
