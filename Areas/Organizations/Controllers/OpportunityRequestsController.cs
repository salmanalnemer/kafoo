using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Models.Organizations;
using Kafo.Web.ViewModels.Organizations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Organizations.Controllers;

[Area("Organizations")]
public class OpportunityRequestsController : Controller
{
    private readonly ApplicationDbContext _context;

    public OpportunityRequestsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var organizationId = GetOrganizationId();

        var items = await _context.OpportunityRequests
            .AsNoTracking()
            .Where(x => x.OrganizationAccountId == organizationId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(items);
    }

    public IActionResult Create()
    {
        return View(new OpportunityRequestFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityRequestFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var organizationId = GetOrganizationId();

        var request = new OpportunityRequest
        {
            OrganizationAccountId = organizationId,
            Status = "جديد",
            CreatedAt = DateTime.Now
        };

        ApplyFormModel(request, model);

        _context.OpportunityRequests.Add(request);
        _context.OrganizationNotifications.Add(new OrganizationNotification
        {
            OrganizationAccountId = organizationId,
            Title = "تم استقبال طلب الاستقطاب",
            Message = $"تم استقبال طلب {request.OpportunityType}: {request.Title} وسيتم مراجعته من قبل الجمعية.",
            OpportunityRequest = request,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إرسال طلب الاستقطاب بنجاح.";
        return RedirectToAction(nameof(Details), new { id = request.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var organizationId = GetOrganizationId();

        var item = await _context.OpportunityRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationAccountId == organizationId);

        if (item == null)
        {
            return NotFound();
        }

        ViewBag.RequestId = item.Id;
        return View(ToFormModel(item));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OpportunityRequestFormViewModel model)
    {
        var organizationId = GetOrganizationId();

        var item = await _context.OpportunityRequests
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationAccountId == organizationId);

        if (item == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.RequestId = id;
            return View(model);
        }

        ApplyFormModel(item, model);

        _context.OrganizationNotifications.Add(new OrganizationNotification
        {
            OrganizationAccountId = organizationId,
            OpportunityRequestId = item.Id,
            Title = "تم تحديث طلب الاستقطاب",
            Message = $"تم تحديث بيانات طلب {item.OpportunityType}: {item.Title}.",
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حفظ تعديلات طلب الاستقطاب.";
        return RedirectToAction(nameof(Details), new { id = item.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var organizationId = GetOrganizationId();

        var item = await _context.OpportunityRequests
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationAccountId == organizationId);

        if (item == null)
        {
            return NotFound();
        }

        var notifications = await _context.OrganizationNotifications
            .Where(x => x.OpportunityRequestId == item.Id)
            .ToListAsync();

        var evaluations = await _context.OrganizationEvaluations
            .Where(x => x.OpportunityRequestId == item.Id)
            .ToListAsync();

        var candidates = await _context.OpportunityCandidates
            .Where(x => x.OpportunityRequestId == item.Id)
            .ToListAsync();

        _context.OrganizationNotifications.RemoveRange(notifications);
        _context.OrganizationEvaluations.RemoveRange(evaluations);
        _context.OpportunityCandidates.RemoveRange(candidates);
        _context.OpportunityRequests.Remove(item);

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف طلب الاستقطاب نهائيًا.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var organizationId = GetOrganizationId();

        var item = await _context.OpportunityRequests
            .AsNoTracking()
            .Include(x => x.Candidates.OrderByDescending(c => c.CreatedAt))
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationAccountId == organizationId);

        if (item == null)
        {
            return NotFound();
        }

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCandidateStatus(int id, int candidateId, string status, string? notes)
    {
        var organizationId = GetOrganizationId();

        var candidate = await _context.OpportunityCandidates
            .Include(x => x.OpportunityRequest)
            .FirstOrDefaultAsync(x => x.Id == candidateId &&
                                      x.OpportunityRequestId == id &&
                                      x.OpportunityRequest != null &&
                                      x.OpportunityRequest.OrganizationAccountId == organizationId);

        if (candidate == null)
        {
            return NotFound();
        }

        var allowedStatuses = new[] { "تم التواصل", "تمت المقابلة", "تم القبول", "لم يتم القبول" };
        if (!allowedStatuses.Contains(status))
        {
            TempData["Error"] = "حالة المرشح غير صحيحة.";
            return RedirectToAction(nameof(Details), new { id });
        }

        candidate.Status = status;
        candidate.OrganizationNotes = string.IsNullOrWhiteSpace(notes)
            ? candidate.OrganizationNotes
            : notes.Trim();
        candidate.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تحديث حالة المرشح.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static OpportunityRequestFormViewModel ToFormModel(OpportunityRequest item)
    {
        return new OpportunityRequestFormViewModel
        {
            OpportunityType = item.OpportunityType,
            Title = item.Title,
            Description = item.Description,
            AvailableCount = item.AvailableCount,
            City = item.City,
            WorkLocation = item.WorkLocation,
            Qualifications = item.Qualifications,
            Skills = item.Skills,
            SuitableDisabilityTypes = item.SuitableDisabilityTypes,
            WorkNature = item.WorkNature,
            EmploymentType = item.EmploymentType,
            WorkHours = item.WorkHours,
            SalaryAmount = item.SalaryAmount,
            AnnualLeaveDays = item.AnnualLeaveDays
        };
    }

    private static void ApplyFormModel(
        OpportunityRequest item,
        OpportunityRequestFormViewModel model)
    {
        item.OpportunityType = model.OpportunityType.Trim();
        item.Title = model.Title.Trim();
        item.Description = model.Description.Trim();
        item.AvailableCount = model.AvailableCount;
        item.City = Normalize(model.City);
        item.WorkLocation = Normalize(model.WorkLocation);
        item.Qualifications = Normalize(model.Qualifications);
        item.Skills = Normalize(model.Skills);
        item.SuitableDisabilityTypes = Normalize(model.SuitableDisabilityTypes);
        item.WorkNature = string.IsNullOrWhiteSpace(model.WorkNature)
            ? "حضوري"
            : model.WorkNature.Trim();
        item.EmploymentType = Normalize(model.EmploymentType);
        item.WorkHours = Normalize(model.WorkHours);
        item.SalaryAmount = model.SalaryAmount;
        item.AnnualLeaveDays = model.AnnualLeaveDays;
        item.UpdatedAt = DateTime.Now;
    }

    private int GetOrganizationId()
    {
        var value = User.FindFirstValue("KafoOrganizationUserId");
        return int.TryParse(value, out var organizationId) ? organizationId : 0;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
