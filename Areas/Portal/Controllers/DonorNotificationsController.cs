using System.Security.Claims;
using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
public class DonorNotificationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DonorNotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Portal/Donor/Notifications")]
    public async Task<IActionResult> Index()
    {
        var donorId = GetDonorId();

        var items = await _context.DonorNotifications
            .AsNoTracking()
            .Include(x => x.DonorContribution)
            .Where(x => x.DonorAccountId == donorId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View("~/Areas/Portal/Views/DonorNotifications/Index.cshtml", items);
    }

    [HttpPost("/Portal/Donor/Notifications/MarkAllRead")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var donorId = GetDonorId();

        var unread = await _context.DonorNotifications
            .Where(x => x.DonorAccountId == donorId && !x.IsRead)
            .ToListAsync();

        foreach (var item in unread)
            item.IsRead = true;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تعليم جميع الإشعارات كمقروءة.";
        return Redirect("/Portal/Donor/Notifications");
    }

    private int GetDonorId()
    {
        var value = User.FindFirstValue("KafoDonorUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var donorId) ? donorId : 0;
    }
}
