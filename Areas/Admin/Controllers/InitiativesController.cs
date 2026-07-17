using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/Initiatives")]
public class InitiativesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public InitiativesController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q)
    {
        var query = _context.Initiatives.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(x =>
                x.Title.Contains(q) ||
                x.InitiativeType.Contains(q) ||
                (x.Location != null && x.Location.Contains(q)) ||
                (x.TargetGroup != null && x.TargetGroup.Contains(q)) ||
                (x.Description != null && x.Description.Contains(q)));
        }

        var initiatives = await query
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        ViewBag.Search = q ?? "";

        return View("~/Areas/Admin/Views/Initiatives/Index.cshtml", initiatives);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        var model = new Initiative
        {
            InitiativeType = "مبادرة مجتمعية",
            IsActive = true
        };

        return View("~/Areas/Admin/Views/Initiatives/Create.cshtml", model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Initiative model, IFormFile? imageFile)
    {
        ModelState.Remove(nameof(model.ImagePath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/Initiatives/Create.cshtml", model);

        if (imageFile != null && imageFile.Length > 0)
            model.ImagePath = await _files.UploadAsync(imageFile, "initiatives");

        model.Title = model.Title.Trim();
        model.InitiativeType = string.IsNullOrWhiteSpace(model.InitiativeType) ? "مبادرة مجتمعية" : model.InitiativeType.Trim();
        model.Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location.Trim();
        model.TargetGroup = string.IsNullOrWhiteSpace(model.TargetGroup) ? null : model.TargetGroup.Trim();
        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        model.Objectives = string.IsNullOrWhiteSpace(model.Objectives) ? null : model.Objectives.Trim();
        model.RegistrationUrl = string.IsNullOrWhiteSpace(model.RegistrationUrl) ? null : model.RegistrationUrl.Trim();
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;

        _context.Initiatives.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إضافة المبادرة بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var initiative = await _context.Initiatives.FindAsync(id);

        if (initiative == null)
            return NotFound();

        return View("~/Areas/Admin/Views/Initiatives/Edit.cshtml", initiative);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Initiative model, IFormFile? imageFile)
    {
        if (id != model.Id)
            return BadRequest();

        ModelState.Remove(nameof(model.ImagePath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/Initiatives/Edit.cshtml", model);

        var initiative = await _context.Initiatives.FindAsync(id);

        if (initiative == null)
            return NotFound();

        if (imageFile != null && imageFile.Length > 0)
            initiative.ImagePath = await _files.UploadAsync(imageFile, "initiatives");

        initiative.Title = model.Title.Trim();
        initiative.InitiativeType = string.IsNullOrWhiteSpace(model.InitiativeType) ? "مبادرة مجتمعية" : model.InitiativeType.Trim();
        initiative.Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location.Trim();
        initiative.TargetGroup = string.IsNullOrWhiteSpace(model.TargetGroup) ? null : model.TargetGroup.Trim();
        initiative.BeneficiariesCount = model.BeneficiariesCount;
        initiative.StartDate = model.StartDate;
        initiative.EndDate = model.EndDate;
        initiative.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        initiative.Objectives = string.IsNullOrWhiteSpace(model.Objectives) ? null : model.Objectives.Trim();
        initiative.RegistrationUrl = string.IsNullOrWhiteSpace(model.RegistrationUrl) ? null : model.RegistrationUrl.Trim();
        initiative.IsFeatured = model.IsFeatured;
        initiative.IsActive = model.IsActive;
        initiative.DisplayOrder = model.DisplayOrder;
        initiative.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تحديث المبادرة بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Toggle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var initiative = await _context.Initiatives.FindAsync(id);

        if (initiative == null)
            return NotFound();

        initiative.IsActive = !initiative.IsActive;
        initiative.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var initiative = await _context.Initiatives.FindAsync(id);

        if (initiative == null)
            return NotFound();

        _context.Initiatives.Remove(initiative);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف المبادرة بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
