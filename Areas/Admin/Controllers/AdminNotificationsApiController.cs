using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AdminNotificationsApiController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminNotificationsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Admin/NotificationsApi/Summary")]
    public async Task<IActionResult> Summary()
    {
        var pendingRequests = await _context.DonorContributions
            .AsNoTracking()
            .Include(x => x.DonorAccount)
            .Include(x => x.ProgramProject)
            .Where(x => x.Status == "بانتظار الاعتماد")
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .ToListAsync();

        var pendingCount = await _context.DonorContributions
            .AsNoTracking()
            .CountAsync(x => x.Status == "بانتظار الاعتماد");

        var surplusResponses = await _context.DonorSurplusDecisions
            .AsNoTracking()
            .Include(x => x.DonorContribution)
                .ThenInclude(x => x!.DonorAccount)
            .OrderByDescending(x => x.ApprovedAt)
            .Take(5)
            .ToListAsync();

        var latestSentNotifications = await _context.DonorNotifications
            .AsNoTracking()
            .Include(x => x.DonorAccount)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .ToListAsync();

        var items = new List<object>();

        items.AddRange(pendingRequests.Select(x => new
        {
            id = $"donor-request-{x.Id}",
            kind = "donor-request",
            title = "يوجد طلب داعم جديد",
            message = $"{x.DonorAccount?.FullName ?? "داعم"} أرسل دعم: {x.Title} بقيمة {x.TotalAmount:N2} ر.س.",
            createdAt = x.CreatedAt.ToString("yyyy/MM/dd HH:mm"),
            url = $"/Admin/Donors/Details/{x.DonorAccountId}#contribution-{x.Id}",
            isActionRequired = true
        }));

        items.AddRange(surplusResponses.Select(x => new
        {
            id = $"surplus-{x.Id}",
            kind = "surplus-response",
            title = "موافقة فائض دعم من داعم",
            message = $"{x.DonorContribution?.DonorAccount?.FullName ?? "داعم"} اختار: {x.DecisionType} للفائض بقيمة {x.SurplusAmount:N2} ر.س.",
            createdAt = x.ApprovedAt.ToString("yyyy/MM/dd HH:mm"),
            url = x.DonorContribution == null ? "/Admin/Donors" : $"/Admin/Donors/Details/{x.DonorContribution.DonorAccountId}#contribution-{x.DonorContributionId}",
            isActionRequired = false
        }));

        items.AddRange(latestSentNotifications.Select(x => new
        {
            id = $"sent-donor-notification-{x.Id}",
            kind = "sent-donor-notification",
            title = "إشعار مرسل للداعم",
            message = $"{x.DonorAccount?.FullName ?? "داعم"}: {x.Title}",
            createdAt = x.CreatedAt.ToString("yyyy/MM/dd HH:mm"),
            url = $"/Admin/Donors/Details/{x.DonorAccountId}",
            isActionRequired = false
        }));

        var sortedItems = items
            .Take(20)
            .ToList();

        return Json(new
        {
            unreadCount = pendingCount,
            pendingDonorRequests = pendingCount,
            hasNewDonorRequest = pendingCount > 0,
            title = pendingCount > 0 ? "يوجد طلب داعم جديد" : "لا توجد طلبات جديدة",
            items = sortedItems
        });
    }
}
