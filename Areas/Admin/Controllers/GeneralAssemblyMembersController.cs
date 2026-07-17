using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/GeneralAssemblyMembers")]
public class GeneralAssemblyMembersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public GeneralAssemblyMembersController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q)
    {
        var query = _context.GeneralAssemblyMembers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(x =>
                x.FullName.Contains(q) ||
                x.MembershipType.Contains(q) ||
                (x.PositionTitle != null && x.PositionTitle.Contains(q)) ||
                (x.MembershipNumber != null && x.MembershipNumber.Contains(q)) ||
                (x.TermLabel != null && x.TermLabel.Contains(q)));
        }

        var members = await query
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.FullName)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        ViewBag.Search = q ?? "";

        return View("~/Areas/Admin/Views/GeneralAssemblyMembers/Index.cshtml", members);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        var model = new GeneralAssemblyMember
        {
            MembershipType = "عضو جمعية عمومية",
            IsActive = true
        };

        return View("~/Areas/Admin/Views/GeneralAssemblyMembers/Create.cshtml", model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GeneralAssemblyMember model, IFormFile? photoFile)
    {
        ModelState.Remove(nameof(model.PhotoPath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/GeneralAssemblyMembers/Create.cshtml", model);

        if (photoFile != null && photoFile.Length > 0)
            model.PhotoPath = await _files.UploadAsync(photoFile, "general-assembly-members");

        model.FullName = model.FullName.Trim();
        model.MembershipType = string.IsNullOrWhiteSpace(model.MembershipType) ? "عضو جمعية عمومية" : model.MembershipType.Trim();
        model.PositionTitle = string.IsNullOrWhiteSpace(model.PositionTitle) ? null : model.PositionTitle.Trim();
        model.MembershipNumber = string.IsNullOrWhiteSpace(model.MembershipNumber) ? null : model.MembershipNumber.Trim();
        model.TermLabel = string.IsNullOrWhiteSpace(model.TermLabel) ? null : model.TermLabel.Trim();
        model.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;

        _context.GeneralAssemblyMembers.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إضافة عضو الجمعية العمومية بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var member = await _context.GeneralAssemblyMembers.FindAsync(id);

        if (member == null)
            return NotFound();

        return View("~/Areas/Admin/Views/GeneralAssemblyMembers/Edit.cshtml", member);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, GeneralAssemblyMember model, IFormFile? photoFile)
    {
        if (id != model.Id)
            return BadRequest();

        ModelState.Remove(nameof(model.PhotoPath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/GeneralAssemblyMembers/Edit.cshtml", model);

        var member = await _context.GeneralAssemblyMembers.FindAsync(id);

        if (member == null)
            return NotFound();

        if (photoFile != null && photoFile.Length > 0)
            member.PhotoPath = await _files.UploadAsync(photoFile, "general-assembly-members");

        member.FullName = model.FullName.Trim();
        member.MembershipType = string.IsNullOrWhiteSpace(model.MembershipType) ? "عضو جمعية عمومية" : model.MembershipType.Trim();
        member.PositionTitle = string.IsNullOrWhiteSpace(model.PositionTitle) ? null : model.PositionTitle.Trim();
        member.MembershipNumber = string.IsNullOrWhiteSpace(model.MembershipNumber) ? null : model.MembershipNumber.Trim();
        member.TermLabel = string.IsNullOrWhiteSpace(model.TermLabel) ? null : model.TermLabel.Trim();
        member.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
        member.JoinedAt = model.JoinedAt;
        member.IsFeatured = model.IsFeatured;
        member.IsActive = model.IsActive;
        member.DisplayOrder = model.DisplayOrder;
        member.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تحديث بيانات العضو بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Toggle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var member = await _context.GeneralAssemblyMembers.FindAsync(id);

        if (member == null)
            return NotFound();

        member.IsActive = !member.IsActive;
        member.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var member = await _context.GeneralAssemblyMembers.FindAsync(id);

        if (member == null)
            return NotFound();

        _context.GeneralAssemblyMembers.Remove(member);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف العضو بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
