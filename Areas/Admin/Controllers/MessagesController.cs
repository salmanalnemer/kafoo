using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/Messages")]
public class MessagesController : Controller
{
    private readonly ApplicationDbContext _context;

    public MessagesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, string? status, string? type, bool archived = false)
    {
        var query = _context.ContactMessages.AsNoTracking().Where(x => x.IsArchived == archived);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(x =>
                x.FullName.Contains(q) ||
                x.Phone.Contains(q) ||
                x.Subject.Contains(q) ||
                x.MessageBody.Contains(q) ||
                (x.Email != null && x.Email.Contains(q)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            status = status.Trim();
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            type = type.Trim();
            query = query.Where(x => x.MessageType == type);
        }

        var messages = await query
            .OrderByDescending(x => !x.IsRead)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        ViewBag.Search = q ?? "";
        ViewBag.Status = status ?? "";
        ViewBag.Type = type ?? "";
        ViewBag.Archived = archived;

        return View("~/Areas/Admin/Views/Messages/Index.cshtml", messages);
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var message = await _context.ContactMessages.FindAsync(id);

        if (message == null)
            return NotFound();

        if (!message.IsRead)
        {
            message.IsRead = true;
            message.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        return View("~/Areas/Admin/Views/Messages/Details.cshtml", message);
    }

    [HttpPost("UpdateStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? adminNotes)
    {
        var message = await _context.ContactMessages.FindAsync(id);

        if (message == null)
            return NotFound();

        message.Status = string.IsNullOrWhiteSpace(status) ? "جديدة" : status.Trim();
        message.AdminNotes = string.IsNullOrWhiteSpace(adminNotes) ? null : adminNotes.Trim();
        message.IsRead = true;
        message.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تحديث حالة الرسالة.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("Archive/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var message = await _context.ContactMessages.FindAsync(id);

        if (message == null)
            return NotFound();

        message.IsArchived = !message.IsArchived;
        message.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = message.IsArchived ? "تم نقل الرسالة للأرشيف." : "تم استعادة الرسالة من الأرشيف.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var message = await _context.ContactMessages.FindAsync(id);

        if (message == null)
            return NotFound();

        _context.ContactMessages.Remove(message);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف الرسالة بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
