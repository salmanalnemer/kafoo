using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
[Authorize(AuthenticationSchemes = KafoAuthSchemes.Portal)]
public class PortalNotificationsApiController : Controller
{
    private readonly ApplicationDbContext _context;

    public PortalNotificationsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Portal/Notifications/Summary")]
    public async Task<IActionResult> Summary()
    {
        var portalType = User.FindFirstValue("KafoPortalType") ?? string.Empty;

        if (string.Equals(portalType, "Donor", StringComparison.OrdinalIgnoreCase))
            return Ok(await BuildDonorSummaryAsync());

        if (string.Equals(portalType, "Organization", StringComparison.OrdinalIgnoreCase))
            return Ok(await BuildOrganizationSummaryAsync());

        return Unauthorized(new { unreadCount = 0, latestKey = string.Empty, items = Array.Empty<object>() });
    }

    [HttpPost("/Portal/Notifications/MarkAllRead")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var portalType = User.FindFirstValue("KafoPortalType") ?? string.Empty;

        if (string.Equals(portalType, "Donor", StringComparison.OrdinalIgnoreCase))
        {
            var donorId = GetDonorId();
            var unread = await _context.DonorNotifications
                .Where(x => x.DonorAccountId == donorId && !x.IsRead)
                .ToListAsync();

            foreach (var item in unread)
                item.IsRead = true;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, unreadCount = 0 });
        }

        if (string.Equals(portalType, "Organization", StringComparison.OrdinalIgnoreCase))
        {
            var organizationId = GetOrganizationId();
            var unread = await _context.OrganizationNotifications
                .Where(x => x.OrganizationAccountId == organizationId && !x.IsRead)
                .ToListAsync();

            foreach (var item in unread)
                item.IsRead = true;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, unreadCount = 0 });
        }

        return Unauthorized(new { success = false });
    }

    private async Task<object> BuildDonorSummaryAsync()
    {
        var donorId = GetDonorId();
        var unreadCount = await _context.DonorNotifications
            .AsNoTracking()
            .CountAsync(x => x.DonorAccountId == donorId && !x.IsRead);

        var items = await _context.DonorNotifications
            .AsNoTracking()
            .Where(x => x.DonorAccountId == donorId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(7)
            .Select(x => new NotificationItemDto
            {
                Id = x.Id,
                Title = x.Title,
                Message = x.Message,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt,
                CreatedAtText = x.CreatedAt.ToString("yyyy/MM/dd HH:mm"),
                Url = x.DonorContributionId.HasValue
                    ? $"/Portal/Donor/Contributions/Details/{x.DonorContributionId.Value}"
                    : "/Portal/Donor/Notifications"
            })
            .ToListAsync();

        var latest = items.FirstOrDefault();
        var latestKey = latest == null ? string.Empty : $"Donor:{latest.Id}:{latest.CreatedAt.Ticks}";

        return new
        {
            portalType = "Donor",
            unreadCount,
            latestKey,
            items
        };
    }

    private async Task<object> BuildOrganizationSummaryAsync()
    {
        var organizationId = GetOrganizationId();
        var unreadCount = await _context.OrganizationNotifications
            .AsNoTracking()
            .CountAsync(x => x.OrganizationAccountId == organizationId && !x.IsRead);

        var items = await _context.OrganizationNotifications
            .AsNoTracking()
            .Where(x => x.OrganizationAccountId == organizationId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(7)
            .Select(x => new NotificationItemDto
            {
                Id = x.Id,
                Title = x.Title,
                Message = x.Message,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt,
                CreatedAtText = x.CreatedAt.ToString("yyyy/MM/dd HH:mm"),
                Url = x.OpportunityRequestId.HasValue
                    ? $"/Portal/Organization/OpportunityRequests/Details/{x.OpportunityRequestId.Value}"
                    : "/Portal/Organization/Notifications"
            })
            .ToListAsync();

        var latest = items.FirstOrDefault();
        var latestKey = latest == null ? string.Empty : $"Organization:{latest.Id}:{latest.CreatedAt.Ticks}";

        return new
        {
            portalType = "Organization",
            unreadCount,
            latestKey,
            items
        };
    }

    private int GetDonorId()
    {
        var value = User.FindFirstValue("KafoDonorUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }

    private int GetOrganizationId()
    {
        var value = User.FindFirstValue("KafoOrganizationUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }

    private sealed class NotificationItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAtText { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
