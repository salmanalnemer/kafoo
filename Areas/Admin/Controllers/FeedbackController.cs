using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class FeedbackController : Controller
{
    private readonly ApplicationDbContext _context;

    public FeedbackController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Admin/Feedback")]
    public async Task<IActionResult> Index(string? q, string? type, string? status, bool archived = false)
    {
        var query = _context.FeedbackEntries
            .AsNoTracking()
            .Where(x => x.IsArchived == archived);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(x =>
                x.Subject.Contains(q) ||
                x.FeedbackBody.Contains(q) ||
                x.FeedbackType.Contains(q) ||
                (x.RelatedService != null && x.RelatedService.Contains(q)) ||
                (x.FullName != null && x.FullName.Contains(q)) ||
                (x.Phone != null && x.Phone.Contains(q)) ||
                (x.Email != null && x.Email.Contains(q)));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            type = type.Trim();
            query = query.Where(x => x.FeedbackType == type);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            status = status.Trim();
            query = query.Where(x => x.Status == status);
        }

        var entries = await query
            .OrderByDescending(x => !x.IsRead)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        ViewBag.Search = q ?? "";
        ViewBag.Type = type ?? "";
        ViewBag.Status = status ?? "";
        ViewBag.Archived = archived;

        return View("~/Areas/Admin/Views/FeedbackEntries/Index.cshtml", entries);
    }

    [HttpGet("/Admin/Feedback/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var entry = await _context.FeedbackEntries.FindAsync(id);

        if (entry == null)
            return NotFound();

        if (!entry.IsRead)
        {
            entry.IsRead = true;
            entry.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        return View("~/Areas/Admin/Views/FeedbackEntries/Details.cshtml", entry);
    }

    [HttpPost("/Admin/Feedback/UpdateStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? adminNotes)
    {
        var entry = await _context.FeedbackEntries.FindAsync(id);

        if (entry == null)
            return NotFound();

        entry.Status = string.IsNullOrWhiteSpace(status) ? "جديدة" : status.Trim();
        entry.AdminNotes = string.IsNullOrWhiteSpace(adminNotes) ? null : adminNotes.Trim();
        entry.IsRead = true;
        entry.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تحديث حالة التغذية الراجعة.";
        return Redirect($"/Admin/Feedback/Details/{id}");
    }

    [HttpPost("/Admin/Feedback/Archive/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var entry = await _context.FeedbackEntries.FindAsync(id);

        if (entry == null)
            return NotFound();

        entry.IsArchived = !entry.IsArchived;
        entry.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = entry.IsArchived ? "تم نقل التغذية الراجعة للأرشيف." : "تم استعادتها من الأرشيف.";
        return Redirect("/Admin/Feedback");
    }

    [HttpPost("/Admin/Feedback/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var entry = await _context.FeedbackEntries.FindAsync(id);

        if (entry == null)
            return NotFound();

        _context.FeedbackEntries.Remove(entry);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف التغذية الراجعة بنجاح.";
        return Redirect("/Admin/Feedback");
    }
}
