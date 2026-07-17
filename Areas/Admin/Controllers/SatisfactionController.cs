using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class SatisfactionController : Controller
{
    private readonly ApplicationDbContext _context;

    public SatisfactionController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Admin/Satisfaction")]
    public async Task<IActionResult> Index(string? q, string? level, bool archived = false)
    {
        var query = _context.SatisfactionResponses
            .AsNoTracking()
            .Where(x => x.IsArchived == archived);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(x =>
                x.ServiceName.Contains(q) ||
                x.BeneficiaryType.Contains(q) ||
                (x.FullName != null && x.FullName.Contains(q)) ||
                (x.Phone != null && x.Phone.Contains(q)) ||
                (x.Email != null && x.Email.Contains(q)) ||
                (x.Suggestions != null && x.Suggestions.Contains(q)));
        }

        if (!string.IsNullOrWhiteSpace(level))
        {
            level = level.Trim();
            query = query.Where(x => x.SatisfactionLevel == level);
        }

        var responses = await query
            .OrderByDescending(x => !x.IsRead)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        ViewBag.Search = q ?? "";
        ViewBag.Level = level ?? "";
        ViewBag.Archived = archived;

        return View("~/Areas/Admin/Views/Feedback/Index.cshtml", responses);
    }

    [HttpGet("/Admin/Satisfaction/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var response = await _context.SatisfactionResponses.FindAsync(id);

        if (response == null)
            return NotFound();

        if (!response.IsRead)
        {
            response.IsRead = true;
            response.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        return View("~/Areas/Admin/Views/Feedback/Details.cshtml", response);
    }

    [HttpPost("/Admin/Satisfaction/SaveNotes/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveNotes(int id, string? adminNotes)
    {
        var response = await _context.SatisfactionResponses.FindAsync(id);

        if (response == null)
            return NotFound();

        response.AdminNotes = string.IsNullOrWhiteSpace(adminNotes) ? null : adminNotes.Trim();
        response.IsRead = true;
        response.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حفظ ملاحظات الإدارة.";
        return Redirect($"/Admin/Satisfaction/Details/{id}");
    }

    [HttpPost("/Admin/Satisfaction/Archive/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var response = await _context.SatisfactionResponses.FindAsync(id);

        if (response == null)
            return NotFound();

        response.IsArchived = !response.IsArchived;
        response.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = response.IsArchived ? "تم نقل الرد للأرشيف." : "تم استعادة الرد من الأرشيف.";
        return Redirect("/Admin/Satisfaction");
    }

    [HttpPost("/Admin/Satisfaction/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _context.SatisfactionResponses.FindAsync(id);

        if (response == null)
            return NotFound();

        _context.SatisfactionResponses.Remove(response);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف رد قياس الرضا بنجاح.";
        return Redirect("/Admin/Satisfaction");
    }
}
