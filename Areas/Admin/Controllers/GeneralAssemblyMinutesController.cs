using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/GeneralAssemblyMinutes")]
public class GeneralAssemblyMinutesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public GeneralAssemblyMinutesController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q)
    {
        var query = _context.GeneralAssemblyMinutes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(x =>
                x.Title.Contains(q) ||
                x.MeetingType.Contains(q) ||
                (x.MeetingNumber != null && x.MeetingNumber.Contains(q)) ||
                (x.FiscalYear != null && x.FiscalYear.Contains(q)) ||
                (x.Summary != null && x.Summary.Contains(q)));
        }

        var minutes = await query
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.MeetingDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        ViewBag.Search = q ?? "";

        return View("~/Areas/Admin/Views/GeneralAssemblyMinutes/Index.cshtml", minutes);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        var model = new GeneralAssemblyMinute
        {
            MeetingType = "اجتماع الجمعية العمومية",
            FiscalYear = DateTime.Now.Year.ToString(),
            MeetingDate = DateTime.Now,
            IsActive = true
        };

        return View("~/Areas/Admin/Views/GeneralAssemblyMinutes/Create.cshtml", model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GeneralAssemblyMinute model, IFormFile? minuteFile, IFormFile? coverFile)
    {
        ModelState.Remove(nameof(model.FilePath));
        ModelState.Remove(nameof(model.CoverImagePath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/GeneralAssemblyMinutes/Create.cshtml", model);

        if (minuteFile != null && minuteFile.Length > 0)
            model.FilePath = await _files.UploadAsync(minuteFile, "general-assembly-minutes");

        if (coverFile != null && coverFile.Length > 0)
            model.CoverImagePath = await _files.UploadAsync(coverFile, "general-assembly-minutes-covers");

        model.Title = model.Title.Trim();
        model.MeetingType = string.IsNullOrWhiteSpace(model.MeetingType) ? "اجتماع الجمعية العمومية" : model.MeetingType.Trim();
        model.MeetingNumber = string.IsNullOrWhiteSpace(model.MeetingNumber) ? null : model.MeetingNumber.Trim();
        model.FiscalYear = string.IsNullOrWhiteSpace(model.FiscalYear) ? null : model.FiscalYear.Trim();
        model.Summary = string.IsNullOrWhiteSpace(model.Summary) ? null : model.Summary.Trim();
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;

        _context.GeneralAssemblyMinutes.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إضافة محضر الجمعية العمومية بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var minute = await _context.GeneralAssemblyMinutes.FindAsync(id);

        if (minute == null)
            return NotFound();

        return View("~/Areas/Admin/Views/GeneralAssemblyMinutes/Edit.cshtml", minute);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, GeneralAssemblyMinute model, IFormFile? minuteFile, IFormFile? coverFile)
    {
        if (id != model.Id)
            return BadRequest();

        ModelState.Remove(nameof(model.FilePath));
        ModelState.Remove(nameof(model.CoverImagePath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/GeneralAssemblyMinutes/Edit.cshtml", model);

        var minute = await _context.GeneralAssemblyMinutes.FindAsync(id);

        if (minute == null)
            return NotFound();

        if (minuteFile != null && minuteFile.Length > 0)
            minute.FilePath = await _files.UploadAsync(minuteFile, "general-assembly-minutes");

        if (coverFile != null && coverFile.Length > 0)
            minute.CoverImagePath = await _files.UploadAsync(coverFile, "general-assembly-minutes-covers");

        minute.Title = model.Title.Trim();
        minute.MeetingType = string.IsNullOrWhiteSpace(model.MeetingType) ? "اجتماع الجمعية العمومية" : model.MeetingType.Trim();
        minute.MeetingNumber = string.IsNullOrWhiteSpace(model.MeetingNumber) ? null : model.MeetingNumber.Trim();
        minute.FiscalYear = string.IsNullOrWhiteSpace(model.FiscalYear) ? null : model.FiscalYear.Trim();
        minute.MeetingDate = model.MeetingDate;
        minute.AttendeesCount = model.AttendeesCount;
        minute.Summary = string.IsNullOrWhiteSpace(model.Summary) ? null : model.Summary.Trim();
        minute.IsFeatured = model.IsFeatured;
        minute.IsActive = model.IsActive;
        minute.DisplayOrder = model.DisplayOrder;
        minute.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تحديث المحضر بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Toggle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var minute = await _context.GeneralAssemblyMinutes.FindAsync(id);

        if (minute == null)
            return NotFound();

        minute.IsActive = !minute.IsActive;
        minute.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var minute = await _context.GeneralAssemblyMinutes.FindAsync(id);

        if (minute == null)
            return NotFound();

        _context.GeneralAssemblyMinutes.Remove(minute);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف المحضر بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
