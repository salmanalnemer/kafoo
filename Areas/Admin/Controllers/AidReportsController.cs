using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/AidReports")]
[Route("Admin/AssistanceReports")]
public class AidReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public AidReportsController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q)
    {
        var query = _context.AidReports.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(x =>
                x.Title.Contains(q) ||
                x.ReportType.Contains(q) ||
                x.PeriodLabel.Contains(q) ||
                (x.Description != null && x.Description.Contains(q)));
        }

        var reports = await query
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        ViewBag.Search = q ?? "";

        return View("~/Areas/Admin/Views/AidReports/Index.cshtml", reports);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        var model = new AidReport
        {
            ReportType = "تقرير مساعدات",
            PeriodLabel = DateTime.Now.ToString("yyyy/MM"),
            PublishedAt = DateTime.Now,
            IsActive = true
        };

        return View("~/Areas/Admin/Views/AidReports/Create.cshtml", model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AidReport model, IFormFile? reportFile, IFormFile? coverFile)
    {
        ModelState.Remove(nameof(model.FilePath));
        ModelState.Remove(nameof(model.CoverImagePath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/AidReports/Create.cshtml", model);

        if (reportFile != null && reportFile.Length > 0)
            model.FilePath = await _files.UploadAsync(reportFile, "aid-reports");

        if (coverFile != null && coverFile.Length > 0)
            model.CoverImagePath = await _files.UploadAsync(coverFile, "aid-report-covers");

        model.Title = model.Title.Trim();
        model.ReportType = string.IsNullOrWhiteSpace(model.ReportType) ? "تقرير مساعدات" : model.ReportType.Trim();
        model.PeriodLabel = string.IsNullOrWhiteSpace(model.PeriodLabel) ? "" : model.PeriodLabel.Trim();
        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;

        _context.AidReports.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إضافة تقرير المساعدات بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var report = await _context.AidReports.FindAsync(id);

        if (report == null)
            return NotFound();

        return View("~/Areas/Admin/Views/AidReports/Edit.cshtml", report);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AidReport model, IFormFile? reportFile, IFormFile? coverFile)
    {
        if (id != model.Id)
            return BadRequest();

        ModelState.Remove(nameof(model.FilePath));
        ModelState.Remove(nameof(model.CoverImagePath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/AidReports/Edit.cshtml", model);

        var report = await _context.AidReports.FindAsync(id);

        if (report == null)
            return NotFound();

        if (reportFile != null && reportFile.Length > 0)
            report.FilePath = await _files.UploadAsync(reportFile, "aid-reports");

        if (coverFile != null && coverFile.Length > 0)
            report.CoverImagePath = await _files.UploadAsync(coverFile, "aid-report-covers");

        report.Title = model.Title.Trim();
        report.ReportType = string.IsNullOrWhiteSpace(model.ReportType) ? "تقرير مساعدات" : model.ReportType.Trim();
        report.PeriodLabel = string.IsNullOrWhiteSpace(model.PeriodLabel) ? "" : model.PeriodLabel.Trim();
        report.BeneficiariesCount = model.BeneficiariesCount;
        report.FamiliesCount = model.FamiliesCount;
        report.TotalAmount = model.TotalAmount;
        report.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        report.PublishedAt = model.PublishedAt;
        report.IsFeatured = model.IsFeatured;
        report.IsActive = model.IsActive;
        report.DisplayOrder = model.DisplayOrder;
        report.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تحديث تقرير المساعدات بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Toggle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var report = await _context.AidReports.FindAsync(id);

        if (report == null)
            return NotFound();

        report.IsActive = !report.IsActive;
        report.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var report = await _context.AidReports.FindAsync(id);

        if (report == null)
            return NotFound();

        _context.AidReports.Remove(report);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف التقرير بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
