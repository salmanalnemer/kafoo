using Kafo.Web.Data;
using Kafo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class HomeStatisticsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeStatisticsController> _logger;

    public HomeStatisticsController(
        ApplicationDbContext context,
        ILogger<HomeStatisticsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _context.HomeStatistics
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        return View(items);
    }

    public IActionResult Create()
    {
        return View(new HomeStatistic
        {
            IsActive = true,
            DisplayOrder = 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HomeStatistic model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            model.Title = model.Title.Trim();
            model.Value = model.Value.Trim();
            model.Icon = string.IsNullOrWhiteSpace(model.Icon) ? null : model.Icon.Trim();
            model.CreatedAt = DateTime.Now;

            _context.HomeStatistics.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة الإحصائية بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating home statistic");
            ModelState.AddModelError(string.Empty, "حدث خطأ أثناء حفظ الإحصائية.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.HomeStatistics.FindAsync(id);

        if (item == null)
            return NotFound();

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, HomeStatistic model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var item = await _context.HomeStatistics.FindAsync(id);

        if (item == null)
            return NotFound();

        try
        {
            item.Title = model.Title.Trim();
            item.Value = model.Value.Trim();
            item.Icon = string.IsNullOrWhiteSpace(model.Icon) ? null : model.Icon.Trim();
            item.DisplayOrder = model.DisplayOrder;
            item.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تعديل الإحصائية بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while editing home statistic {StatisticId}", id);
            ModelState.AddModelError(string.Empty, "حدث خطأ أثناء تعديل الإحصائية.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var item = await _context.HomeStatistics.FindAsync(id);

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
        var item = await _context.HomeStatistics.FindAsync(id);

        if (item == null)
            return NotFound();

        _context.HomeStatistics.Remove(item);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف الإحصائية بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
