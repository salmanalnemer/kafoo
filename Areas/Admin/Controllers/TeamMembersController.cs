using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class TeamMembersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILogger<TeamMembersController> _logger;

    public TeamMembersController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILogger<TeamMembersController> logger)
    {
        _context = context;
        _files = files;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var members = await _context.TeamMembers
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        return View(members);
    }

    public IActionResult Create()
    {
        return View(new TeamMember
        {
            IsActive = true,
            DisplayOrder = 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeamMember model, IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            if (imageFile != null)
                model.ImagePath = await _files.UploadAsync(imageFile, "team");

            model.FullName = model.FullName.Trim();
            model.JobTitle = model.JobTitle.Trim();
            model.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
            model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            model.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            _context.TeamMembers.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة عضو الفريق بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating team member");
            ModelState.AddModelError(string.Empty, "حدث خطأ أثناء حفظ عضو الفريق.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var member = await _context.TeamMembers.FindAsync(id);

        if (member == null)
            return NotFound();

        return View(member);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TeamMember model, IFormFile? imageFile)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var member = await _context.TeamMembers.FindAsync(id);

        if (member == null)
            return NotFound();

        try
        {
            if (imageFile != null)
            {
                _files.Delete(member.ImagePath);
                member.ImagePath = await _files.UploadAsync(imageFile, "team");
            }

            member.FullName = model.FullName.Trim();
            member.JobTitle = model.JobTitle.Trim();
            member.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
            member.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            member.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
            member.DisplayOrder = model.DisplayOrder;
            member.IsActive = model.IsActive;
            member.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تعديل عضو الفريق بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while editing team member {TeamMemberId}", id);
            ModelState.AddModelError(string.Empty, "حدث خطأ أثناء تعديل عضو الفريق.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var member = await _context.TeamMembers.FindAsync(id);

        if (member == null)
            return NotFound();

        member.IsActive = !member.IsActive;
        member.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var member = await _context.TeamMembers.FindAsync(id);

        if (member == null)
            return NotFound();

        _files.Delete(member.ImagePath);
        _context.TeamMembers.Remove(member);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف عضو الفريق بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
