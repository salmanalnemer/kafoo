using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/SuccessPartners")]
public class SuccessPartnersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public SuccessPartnersController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q)
    {
        var query = _context.SuccessPartners.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(x =>
                x.Name.Contains(q) ||
                x.PartnerType.Contains(q) ||
                (x.Description != null && x.Description.Contains(q)));
        }

        var partners = await query
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        ViewBag.Search = q ?? "";

        return View("~/Areas/Admin/Views/SuccessPartners/Index.cshtml", partners);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        var model = new SuccessPartner
        {
            PartnerType = "شريك نجاح",
            IsActive = true
        };

        return View("~/Areas/Admin/Views/SuccessPartners/Create.cshtml", model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SuccessPartner model, IFormFile? logoFile)
    {
        ModelState.Remove(nameof(model.LogoPath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/SuccessPartners/Create.cshtml", model);

        if (logoFile != null && logoFile.Length > 0)
            model.LogoPath = await _files.UploadAsync(logoFile, "success-partners");

        model.Name = model.Name.Trim();
        model.PartnerType = string.IsNullOrWhiteSpace(model.PartnerType) ? "شريك نجاح" : model.PartnerType.Trim();
        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        model.WebsiteUrl = string.IsNullOrWhiteSpace(model.WebsiteUrl) ? null : model.WebsiteUrl.Trim();
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;

        _context.SuccessPartners.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إضافة شريك النجاح بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var partner = await _context.SuccessPartners.FindAsync(id);

        if (partner == null)
            return NotFound();

        return View("~/Areas/Admin/Views/SuccessPartners/Edit.cshtml", partner);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SuccessPartner model, IFormFile? logoFile)
    {
        if (id != model.Id)
            return BadRequest();

        ModelState.Remove(nameof(model.LogoPath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/SuccessPartners/Edit.cshtml", model);

        var partner = await _context.SuccessPartners.FindAsync(id);

        if (partner == null)
            return NotFound();

        if (logoFile != null && logoFile.Length > 0)
            partner.LogoPath = await _files.UploadAsync(logoFile, "success-partners");

        partner.Name = model.Name.Trim();
        partner.PartnerType = string.IsNullOrWhiteSpace(model.PartnerType) ? "شريك نجاح" : model.PartnerType.Trim();
        partner.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        partner.WebsiteUrl = string.IsNullOrWhiteSpace(model.WebsiteUrl) ? null : model.WebsiteUrl.Trim();
        partner.IsFeatured = model.IsFeatured;
        partner.IsActive = model.IsActive;
        partner.DisplayOrder = model.DisplayOrder;
        partner.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تحديث شريك النجاح بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Toggle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var partner = await _context.SuccessPartners.FindAsync(id);

        if (partner == null)
            return NotFound();

        partner.IsActive = !partner.IsActive;
        partner.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var partner = await _context.SuccessPartners.FindAsync(id);

        if (partner == null)
            return NotFound();

        _context.SuccessPartners.Remove(partner);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف شريك النجاح بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
