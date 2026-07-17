using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
[Authorize(AuthenticationSchemes = KafoAuthSchemes.Portal)]
public class OrganizationNotificationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public OrganizationNotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Portal/Organization/Notifications")]
    public async Task<IActionResult> Index()
    {
        var organizationId = GetOrganizationId();

        var items = await _context.OrganizationNotifications
            .AsNoTracking()
            .Include(x => x.OpportunityRequest)
            .Where(x => x.OrganizationAccountId == organizationId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View("~/Areas/Portal/Views/OrganizationNotifications/Index.cshtml", items);
    }

    [HttpPost("/Portal/Organization/Notifications/MarkAllRead")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var organizationId = GetOrganizationId();

        var unread = await _context.OrganizationNotifications
            .Where(x => x.OrganizationAccountId == organizationId && !x.IsRead)
            .ToListAsync();

        foreach (var item in unread)
            item.IsRead = true;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تعليم جميع الإشعارات كمقروءة.";
        return Redirect("/Portal/Organization/Notifications");
    }

    private int GetOrganizationId()
    {
        var value = User.FindFirstValue("KafoOrganizationUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }
}
