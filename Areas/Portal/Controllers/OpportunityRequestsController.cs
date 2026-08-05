using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Models.Organizations;
using Kafo.Web.Security;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
[Authorize(AuthenticationSchemes = KafoAuthSchemes.Portal)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OpportunityRequestsController : Controller
{
    private static readonly HashSet<string> AllowedOpportunityTypes =
        new(["توظيف", "تدريب تعاوني", "تطوع"], StringComparer.Ordinal);

    private static readonly HashSet<string> AllowedWorkNatures =
        new(["حضوري", "عن بُعد", "هجين"], StringComparer.Ordinal);

    private static readonly HashSet<string> AllowedEmploymentTypes =
        new(["دوام كامل", "دوام جزئي", "لا ينطبق"], StringComparer.Ordinal);

    private static readonly HashSet<string> AllowedCandidateStatuses =
        new(["تم التواصل", "تمت المقابلة", "تم القبول", "لم يتم القبول"], StringComparer.Ordinal);

    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILogger<OpportunityRequestsController> _logger;

    public OpportunityRequestsController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILogger<OpportunityRequestsController> logger)
    {
        _context = context;
        _files = files;
        _logger = logger;
    }

    [HttpGet("/Portal/Organization/OpportunityRequests")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(out var organizationId))
            return Forbid();

        var items = await _context.OpportunityRequests
            .AsNoTracking()
            .Where(x => x.OrganizationAccountId == organizationId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return View("~/Areas/Portal/Views/OpportunityRequests/Index.cshtml", items);
    }

    [HttpGet("/Portal/Organization/OpportunityRequests/Create")]
    public IActionResult Create()
        => View(
            "~/Areas/Portal/Views/OpportunityRequests/Create.cshtml",
            new OpportunityRequestFormViewModel());

    [HttpPost("/Portal/Organization/OpportunityRequests/Create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(128 * 1024)]
    public async Task<IActionResult> Create(
        OpportunityRequestFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(out var organizationId))
            return Forbid();

        ValidateFormModel(model);
        if (!ModelState.IsValid)
            return View("~/Areas/Portal/Views/OpportunityRequests/Create.cshtml", model);

        var request = new OpportunityRequest
        {
            OrganizationAccountId = organizationId,
            Status = "جديد",
            AdminNotes = null,
            OrganizationNotes = null,
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

        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "تم إرسال طلب الاستقطاب بنجاح.";
        return Redirect($"/Portal/Organization/OpportunityRequests/Details/{request.Id}");
    }

    [HttpGet("/Portal/Organization/OpportunityRequests/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(out var organizationId))
            return Forbid();

        var item = await _context.OpportunityRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id && x.OrganizationAccountId == organizationId,
                cancellationToken);

        if (item == null)
            return NotFound();

        ViewBag.RequestId = item.Id;
        return View(
            "~/Areas/Portal/Views/OpportunityRequests/Edit.cshtml",
            ToFormModel(item));
    }

    [HttpPost("/Portal/Organization/OpportunityRequests/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(128 * 1024)]
    public async Task<IActionResult> Edit(
        int id,
        OpportunityRequestFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(out var organizationId))
            return Forbid();

        var item = await _context.OpportunityRequests
            .FirstOrDefaultAsync(
                x => x.Id == id && x.OrganizationAccountId == organizationId,
                cancellationToken);

        if (item == null)
            return NotFound();

        ValidateFormModel(model);
        if (!ModelState.IsValid)
        {
            ViewBag.RequestId = id;
            return View("~/Areas/Portal/Views/OpportunityRequests/Edit.cshtml", model);
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

        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "تم حفظ تعديلات طلب الاستقطاب.";
        return Redirect($"/Portal/Organization/OpportunityRequests/Details/{item.Id}");
    }

    [HttpPost("/Portal/Organization/OpportunityRequests/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(out var organizationId))
            return Forbid();

        var item = await _context.OpportunityRequests
            .FirstOrDefaultAsync(
                x => x.Id == id && x.OrganizationAccountId == organizationId,
                cancellationToken);

        if (item == null)
            return NotFound();

        var notifications = await _context.OrganizationNotifications
            .Where(x => x.OpportunityRequestId == item.Id)
            .ToListAsync(cancellationToken);

        var evaluations = await _context.OrganizationEvaluations
            .Where(x => x.OpportunityRequestId == item.Id)
            .ToListAsync(cancellationToken);

        var candidates = await _context.OpportunityCandidates
            .Where(x => x.OpportunityRequestId == item.Id)
            .ToListAsync(cancellationToken);

        var managedFiles = candidates
            .Select(x => x.CvFilePath)
            .Append(item.JobDescriptionFile)
            .Append(item.AdditionalAttachment)
            .Append(item.OpportunityImage)
            .Append(item.IntroVideo)
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _context.OrganizationNotifications.RemoveRange(notifications);
            _context.OrganizationEvaluations.RemoveRange(evaluations);
            _context.OpportunityCandidates.RemoveRange(candidates);
            _context.OpportunityRequests.Remove(item);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        foreach (var filePath in managedFiles)
            _files.Delete(filePath);

        _logger.LogInformation(
            "Organization {OrganizationId} deleted opportunity request {RequestId} and {FileCount} managed files",
            organizationId,
            id,
            managedFiles.Length);

        TempData["Success"] = "تم حذف طلب الاستقطاب ومرفقاته نهائيًا.";
        return Redirect("/Portal/Organization/OpportunityRequests");
    }

    [HttpGet("/Portal/Organization/OpportunityRequests/Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(out var organizationId))
            return Forbid();

        var item = await _context.OpportunityRequests
            .AsNoTracking()
            .Include(x => x.Candidates.OrderByDescending(c => c.CreatedAt))
            .FirstOrDefaultAsync(
                x => x.Id == id && x.OrganizationAccountId == organizationId,
                cancellationToken);

        if (item == null)
            return NotFound();

        return View("~/Areas/Portal/Views/OpportunityRequests/Details.cshtml", item);
    }

    [HttpPost("/Portal/Organization/OpportunityRequests/UpdateCandidateStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> UpdateCandidateStatus(
        int id,
        int candidateId,
        string? status,
        string? notes,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(out var organizationId))
            return Forbid();

        var normalizedStatus = (status ?? string.Empty).Trim();
        if (!AllowedCandidateStatuses.Contains(normalizedStatus))
        {
            TempData["Error"] = "حالة المرشح غير صحيحة.";
            return Redirect($"/Portal/Organization/OpportunityRequests/Details/{id}");
        }

        var normalizedNotes = Normalize(notes);
        if (normalizedNotes is { Length: > 1200 })
        {
            TempData["Error"] = "ملاحظات المرشح تتجاوز الحد المسموح.";
            return Redirect($"/Portal/Organization/OpportunityRequests/Details/{id}");
        }

        var candidate = await _context.OpportunityCandidates
            .Include(x => x.OpportunityRequest)
            .FirstOrDefaultAsync(
                x => x.Id == candidateId &&
                     x.OpportunityRequestId == id &&
                     x.OpportunityRequest != null &&
                     x.OpportunityRequest.OrganizationAccountId == organizationId,
                cancellationToken);

        if (candidate == null)
            return NotFound();

        candidate.Status = normalizedStatus;
        if (normalizedNotes != null)
            candidate.OrganizationNotes = normalizedNotes;
        candidate.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "تم تحديث حالة المرشح.";
        return Redirect($"/Portal/Organization/OpportunityRequests/Details/{id}");
    }

    private void ValidateFormModel(OpportunityRequestFormViewModel model)
    {
        model.OpportunityType = (model.OpportunityType ?? string.Empty).Trim();
        model.Title = (model.Title ?? string.Empty).Trim();
        model.Description = (model.Description ?? string.Empty).Trim();
        model.WorkNature = (model.WorkNature ?? string.Empty).Trim();
        model.EmploymentType = Normalize(model.EmploymentType);
        model.City = Normalize(model.City);
        model.WorkLocation = Normalize(model.WorkLocation);
        model.Qualifications = Normalize(model.Qualifications);
        model.Skills = Normalize(model.Skills);
        model.SuitableDisabilityTypes = Normalize(model.SuitableDisabilityTypes);
        model.WorkHours = Normalize(model.WorkHours);

        if (!AllowedOpportunityTypes.Contains(model.OpportunityType))
            ModelState.AddModelError(nameof(model.OpportunityType), "نوع الفرصة غير صالح.");

        if (!AllowedWorkNatures.Contains(model.WorkNature))
            ModelState.AddModelError(nameof(model.WorkNature), "طبيعة العمل غير صالحة.");

        if (model.EmploymentType != null && !AllowedEmploymentTypes.Contains(model.EmploymentType))
            ModelState.AddModelError(nameof(model.EmploymentType), "نوع الدوام غير صالح.");

        if (string.IsNullOrWhiteSpace(model.Title))
            ModelState.AddModelError(nameof(model.Title), "المسمى أو اسم الفرصة مطلوب.");

        if (string.IsNullOrWhiteSpace(model.Description))
            ModelState.AddModelError(nameof(model.Description), "الوصف أو المهام مطلوبة.");
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
        item.OpportunityType = model.OpportunityType;
        item.Title = model.Title;
        item.Description = model.Description;
        item.AvailableCount = model.AvailableCount;
        item.City = model.City;
        item.WorkLocation = model.WorkLocation;
        item.Qualifications = model.Qualifications;
        item.Skills = model.Skills;
        item.SuitableDisabilityTypes = model.SuitableDisabilityTypes;
        item.WorkNature = model.WorkNature;
        item.EmploymentType = model.EmploymentType;
        item.WorkHours = model.WorkHours;
        item.SalaryAmount = model.SalaryAmount;
        item.AnnualLeaveDays = model.AnnualLeaveDays;
        item.UpdatedAt = DateTime.Now;
    }

    private bool TryGetOrganizationId(out int organizationId)
    {
        var value = User.FindFirstValue("KafoOrganizationUserId");
        return int.TryParse(value, out organizationId) && organizationId > 0;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
