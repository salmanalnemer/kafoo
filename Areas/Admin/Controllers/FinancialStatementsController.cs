using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/FinancialStatements")]
public class FinancialStatementsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public FinancialStatementsController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q)
    {
        var query = _context.FinancialStatementDocuments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(x =>
                x.Title.Contains(q) ||
                x.Year.Contains(q) ||
                x.Category.Contains(q) ||
                (x.Description != null && x.Description.Contains(q)));
        }

        var documents = await query
            .OrderBy(x => x.Id)
            .ToListAsync();

        ViewBag.Search = q ?? "";

        return View("~/Areas/Admin/Views/FinancialStatements/Index.cshtml", documents);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FinancialStatementDocument model, IFormFile? documentFile)
    {
        ModelState.Remove(nameof(model.FilePath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (documentFile == null || documentFile.Length == 0)
            ModelState.AddModelError(nameof(model.FilePath), "ملف PDF مطلوب.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "تأكد من تعبئة عنوان الملف والسنة والتصنيف وإرفاق ملف PDF.";
            return RedirectToAction(nameof(Index));
        }

        model.FilePath = await _files.UploadAsync(documentFile, "financial-statements");
        model.Title = model.Title.Trim();
        model.Year = string.IsNullOrWhiteSpace(model.Year) ? DateTime.Now.Year.ToString() : model.Year.Trim();
        model.Category = string.IsNullOrWhiteSpace(model.Category) ? "القوائم المالية" : model.Category.Trim();
        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        model.IsActive = true;
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;

        _context.FinancialStatementDocuments.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم رفع ملف القوائم المالية بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Toggle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var document = await _context.FinancialStatementDocuments.FindAsync(id);

        if (document == null)
            return NotFound();

        document.IsActive = !document.IsActive;
        document.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var document = await _context.FinancialStatementDocuments.FindAsync(id);

        if (document == null)
            return NotFound();

        _context.FinancialStatementDocuments.Remove(document);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف الملف بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
