using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Models.Donors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
public class ContributionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ContributionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Portal/Donor/Contributions")]
    public async Task<IActionResult> Index(string? status = null)
    {
        var donorId = GetDonorId();

        var query = _context.DonorContributions
            .AsNoTracking()
            .Include(x => x.ProgramProject)
            .Include(x => x.Certificate)
            .Where(x => x.DonorAccountId == donorId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        ViewBag.SelectedStatus = status;
        ViewBag.TotalAmount = items.Sum(x => x.TotalAmount);
        ViewBag.TotalSpent = items.Sum(x => x.SpentAmount);
        ViewBag.TotalRemaining = items.Sum(x => x.RemainingAmount);
        ViewBag.BeneficiariesCount = items.Sum(x => x.BeneficiariesCount);

        return View("~/Areas/Portal/Views/Contributions/Index.cshtml", items);
    }

    [HttpGet("/Portal/Donor/Contributions/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var donorId = GetDonorId();

        var item = await _context.DonorContributions
            .AsNoTracking()
            .Include(x => x.ProgramProject)
            .Include(x => x.Certificate)
            .Include(x => x.Updates.OrderByDescending(u => u.CreatedAt))
            .Include(x => x.Reports.OrderByDescending(r => r.ReportDate))
            .Include(x => x.SurplusDecisions.OrderByDescending(s => s.ApprovedAt))
            .FirstOrDefaultAsync(x => x.Id == id && x.DonorAccountId == donorId);

        if (item == null)
            return NotFound();

        return View("~/Areas/Portal/Views/Contributions/Details.cshtml", item);
    }

    [HttpGet("/Portal/Donor/Contributions/Certificate/{id:int}")]
    public async Task<IActionResult> Certificate(int id)
    {
        var donorId = GetDonorId();

        var certificate = await _context.DonorContributionCertificates
            .AsNoTracking()
            .Include(x => x.DonorContribution)
                .ThenInclude(x => x!.ProgramProject)
            .Include(x => x.DonorContribution)
                .ThenInclude(x => x!.DonorAccount)
            .FirstOrDefaultAsync(x => x.DonorContributionId == id && x.DonorContribution!.DonorAccountId == donorId);

        if (certificate == null)
            return NotFound();

        return View("~/Areas/Portal/Views/Contributions/Certificate.cshtml", certificate);
    }

    [HttpPost("/Portal/Donor/Contributions/ApproveSurplus/{id:int}")]
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
            TempData["Error"] = "خيار فائض الدعم غير صحيح.";
            return Redirect($"/Portal/Donor/Contributions/Details/{id}");
        }

        _context.DonorSurplusDecisions.Add(new DonorSurplusDecision
        {
            DonorContributionId = contribution.Id,
            SurplusAmount = contribution.RemainingAmount,
            DecisionType = decisionType,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Status = "موافق عليه",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            ApprovedAt = DateTime.Now
        });

        _context.DonorNotifications.Add(new DonorNotification
        {
            DonorAccountId = donorId,
            DonorContributionId = contribution.Id,
            Title = "تم حفظ موافقة فائض الدعم",
            Message = $"تم تسجيل اختيارك: {decisionType}، وسيتم التعامل معه وفق إجراءات الحوكمة.",
            CreatedAt = DateTime.Now
        });

        contribution.IsSurplusLocked = false;
        contribution.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حفظ موافقتك إلكترونيًا ضمن سجل الحوكمة والشفافية.";
        return Redirect($"/Portal/Donor/Contributions/Details/{id}");
    }

    private int GetDonorId()
    {
        var value = User.FindFirstValue("KafoDonorUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var donorId) ? donorId : 0;
    }
}
