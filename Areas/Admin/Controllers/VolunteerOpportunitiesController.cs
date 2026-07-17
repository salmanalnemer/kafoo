using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/VolunteerOpportunities")]
public class VolunteerOpportunitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public VolunteerOpportunitiesController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q)
    {
        var query = _context.VolunteerOpportunities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(x =>
                x.Title.Contains(q) ||
                x.OpportunityType.Contains(q) ||
                (x.Location != null && x.Location.Contains(q)) ||
                (x.Description != null && x.Description.Contains(q)));
        }

        var opportunities = await query
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        ViewBag.Search = q ?? "";

        return View("~/Areas/Admin/Views/VolunteerOpportunities/Index.cshtml", opportunities);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        var model = new VolunteerOpportunity
        {
            OpportunityType = "فرصة تطوعية",
            IsActive = true
        };

        return View("~/Areas/Admin/Views/VolunteerOpportunities/Create.cshtml", model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VolunteerOpportunity model, IFormFile? imageFile)
    {
        ModelState.Remove(nameof(model.ImagePath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/VolunteerOpportunities/Create.cshtml", model);

        if (imageFile != null && imageFile.Length > 0)
            model.ImagePath = await _files.UploadAsync(imageFile, "volunteer-opportunities");

        model.Title = model.Title.Trim();
        model.OpportunityType = string.IsNullOrWhiteSpace(model.OpportunityType) ? "فرصة تطوعية" : model.OpportunityType.Trim();
        model.Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location.Trim();
        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        model.Requirements = string.IsNullOrWhiteSpace(model.Requirements) ? null : model.Requirements.Trim();
        model.RegistrationUrl = string.IsNullOrWhiteSpace(model.RegistrationUrl) ? null : model.RegistrationUrl.Trim();
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;

        _context.VolunteerOpportunities.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إضافة الفرصة التطوعية بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var opportunity = await _context.VolunteerOpportunities.FindAsync(id);

        if (opportunity == null)
            return NotFound();

        return View("~/Areas/Admin/Views/VolunteerOpportunities/Edit.cshtml", opportunity);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VolunteerOpportunity model, IFormFile? imageFile)
    {
        if (id != model.Id)
            return BadRequest();

        ModelState.Remove(nameof(model.ImagePath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/VolunteerOpportunities/Edit.cshtml", model);

        var opportunity = await _context.VolunteerOpportunities.FindAsync(id);

        if (opportunity == null)
            return NotFound();

        if (imageFile != null && imageFile.Length > 0)
            opportunity.ImagePath = await _files.UploadAsync(imageFile, "volunteer-opportunities");

        opportunity.Title = model.Title.Trim();
        opportunity.OpportunityType = string.IsNullOrWhiteSpace(model.OpportunityType) ? "فرصة تطوعية" : model.OpportunityType.Trim();
        opportunity.Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location.Trim();
        opportunity.StartDate = model.StartDate;
        opportunity.EndDate = model.EndDate;
        opportunity.VolunteerHours = model.VolunteerHours;
        opportunity.SeatsCount = model.SeatsCount;
        opportunity.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        opportunity.Requirements = string.IsNullOrWhiteSpace(model.Requirements) ? null : model.Requirements.Trim();
        opportunity.RegistrationUrl = string.IsNullOrWhiteSpace(model.RegistrationUrl) ? null : model.RegistrationUrl.Trim();
        opportunity.IsRemote = model.IsRemote;
        opportunity.IsFeatured = model.IsFeatured;
        opportunity.IsActive = model.IsActive;
        opportunity.DisplayOrder = model.DisplayOrder;
        opportunity.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تحديث الفرصة التطوعية بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Toggle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var opportunity = await _context.VolunteerOpportunities.FindAsync(id);

        if (opportunity == null)
            return NotFound();

        opportunity.IsActive = !opportunity.IsActive;
        opportunity.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var opportunity = await _context.VolunteerOpportunities.FindAsync(id);

        if (opportunity == null)
            return NotFound();

        _context.VolunteerOpportunities.Remove(opportunity);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف الفرصة التطوعية بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
