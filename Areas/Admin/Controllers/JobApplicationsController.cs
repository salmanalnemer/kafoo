using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/JobApplications")]
public class JobApplicationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public JobApplicationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, string? status)
    {
        var query = _context.JobApplications.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(x =>
                x.FullName.Contains(q) ||
                x.Phone.Contains(q) ||
                x.Email.Contains(q) ||
                (x.NationalId != null && x.NationalId.Contains(q)) ||
                (x.City != null && x.City.Contains(q)) ||
                (x.DesiredJobTitle != null && x.DesiredJobTitle.Contains(q)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            status = status.Trim();
            query = query.Where(x => x.Status == status);
        }

        var applications = await query
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        ViewBag.Search = q ?? "";
        ViewBag.Status = status ?? "";

        return View("~/Areas/Admin/Views/JobApplications/Index.cshtml", applications);
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var application = await _context.JobApplications.FindAsync(id);

        if (application == null)
            return NotFound();

        if (!application.IsRead)
        {
            application.IsRead = true;
            application.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        return View("~/Areas/Admin/Views/JobApplications/Details.cshtml", application);
    }

    [HttpPost("UpdateStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? adminNotes)
    {
        var application = await _context.JobApplications.FindAsync(id);

        if (application == null)
            return NotFound();

        application.Status = string.IsNullOrWhiteSpace(status) ? "جديد" : status.Trim();
        application.AdminNotes = string.IsNullOrWhiteSpace(adminNotes) ? null : adminNotes.Trim();
        application.IsRead = true;
        application.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تحديث حالة طلب التوظيف.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var application = await _context.JobApplications.FindAsync(id);

        if (application == null)
            return NotFound();

        _context.JobApplications.Remove(application);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف طلب التوظيف بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
