using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/BoardMembers")]
public class BoardMembersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public BoardMembersController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q)
    {
        var query = _context.BoardMembers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(x =>
                x.FullName.Contains(q) ||
                x.PositionTitle.Contains(q) ||
                (x.MembershipRole != null && x.MembershipRole.Contains(q)) ||
                (x.BoardTerm != null && x.BoardTerm.Contains(q)) ||
                (x.Email != null && x.Email.Contains(q)) ||
                (x.Phone != null && x.Phone.Contains(q)));
        }

        var members = await query
            .OrderByDescending(x => x.IsChairman)
            .ThenByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.FullName)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        ViewBag.Search = q ?? "";

        return View("~/Areas/Admin/Views/BoardMembers/Index.cshtml", members);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        var model = new BoardMember
        {
            PositionTitle = "عضو مجلس الإدارة",
            IsActive = true
        };

        return View("~/Areas/Admin/Views/BoardMembers/Create.cshtml", model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BoardMember model, IFormFile? photoFile)
    {
        ModelState.Remove(nameof(model.PhotoPath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/BoardMembers/Create.cshtml", model);

        if (photoFile != null && photoFile.Length > 0)
            model.PhotoPath = await _files.UploadAsync(photoFile, "board-members");

        model.FullName = model.FullName.Trim();
        model.PositionTitle = string.IsNullOrWhiteSpace(model.PositionTitle) ? "عضو مجلس الإدارة" : model.PositionTitle.Trim();
        model.MembershipRole = string.IsNullOrWhiteSpace(model.MembershipRole) ? null : model.MembershipRole.Trim();
        model.BoardTerm = string.IsNullOrWhiteSpace(model.BoardTerm) ? null : model.BoardTerm.Trim();
        model.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        model.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;

        _context.BoardMembers.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إضافة عضو مجلس الإدارة بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var member = await _context.BoardMembers.FindAsync(id);

        if (member == null)
            return NotFound();

        return View("~/Areas/Admin/Views/BoardMembers/Edit.cshtml", member);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BoardMember model, IFormFile? photoFile)
    {
        if (id != model.Id)
            return BadRequest();

        ModelState.Remove(nameof(model.PhotoPath));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/BoardMembers/Edit.cshtml", model);

        var member = await _context.BoardMembers.FindAsync(id);

        if (member == null)
            return NotFound();

        if (photoFile != null && photoFile.Length > 0)
            member.PhotoPath = await _files.UploadAsync(photoFile, "board-members");

        member.FullName = model.FullName.Trim();
        member.PositionTitle = string.IsNullOrWhiteSpace(model.PositionTitle) ? "عضو مجلس الإدارة" : model.PositionTitle.Trim();
        member.MembershipRole = string.IsNullOrWhiteSpace(model.MembershipRole) ? null : model.MembershipRole.Trim();
        member.BoardTerm = string.IsNullOrWhiteSpace(model.BoardTerm) ? null : model.BoardTerm.Trim();
        member.TermStartDate = model.TermStartDate;
        member.TermEndDate = model.TermEndDate;
        member.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        member.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        member.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
        member.IsChairman = model.IsChairman;
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
        var member = await _context.BoardMembers.FindAsync(id);

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
        var member = await _context.BoardMembers.FindAsync(id);

        if (member == null)
            return NotFound();

        _context.BoardMembers.Remove(member);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف العضو بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
