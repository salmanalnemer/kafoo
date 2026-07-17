using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Models.Donors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Donor.Controllers;

[Area("Donor")]
public class ContributionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ContributionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var donorId = GetDonorId();

        var items = await _context.DonorContributions
            .AsNoTracking()
            .Include(x => x.ProgramProject)
            .Where(x => x.DonorAccountId == donorId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(items);
    }

    public async Task<IActionResult> Details(int id)
    {
        var donorId = GetDonorId();

        var item = await _context.DonorContributions
            .AsNoTracking()
            .Include(x => x.ProgramProject)
            .Include(x => x.Updates.OrderByDescending(u => u.CreatedAt))
            .Include(x => x.Reports.OrderByDescending(r => r.ReportDate))
            .Include(x => x.SurplusDecisions.OrderByDescending(s => s.ApprovedAt))
            .FirstOrDefaultAsync(x => x.Id == id && x.DonorAccountId == donorId);

        if (item == null)
            return NotFound();

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveSurplus(int id, string decisionType, string? notes)
    {
        var donorId = GetDonorId();

        var contribution = await _context.DonorContributions
            .FirstOrDefaultAsync(x => x.Id == id && x.DonorAccountId == donorId && x.HasSurplus && x.RemainingAmount > 0);

        if (contribution == null)
            return NotFound();

        var allowedDecisions = new[]
        {
            "تحويل الفائض إلى برنامج آخر",
            "توجيهه لمستفيدين آخرين ضمن نفس البرنامج",
            "الاحتفاظ به لدعم مبادرة مستقبلية",
            "التواصل مع الجمعية لاتخاذ الإجراء المناسب"
        };

        if (!allowedDecisions.Contains(decisionType))
        {
            TempData["Error"] = "خيار الفائض غير صحيح.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _context.DonorSurplusDecisions.Add(new DonorSurplusDecision
        {
            DonorContributionId = contribution.Id,
            SurplusAmount = contribution.RemainingAmount,
            DecisionType = decisionType,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            ApprovedAt = DateTime.Now
        });

        contribution.IsSurplusLocked = false;
        contribution.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حفظ موافقتك إلكترونيًا ضمن سجل الحوكمة والشفافية.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private int GetDonorId()
    {
        var value = User.FindFirstValue("KafoDonorUserId");
        return int.TryParse(value, out var donorId) ? donorId : 0;
    }
}
