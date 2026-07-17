using Kafo.Web.Services;
using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Models.Donors;
using Kafo.Web.ViewModels.Donor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
public class DonorProgramsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DonorProgramsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Portal/Donor/Programs")]
    public async Task<IActionResult> Index(string? category = null)
    {
        var query = _context.ProgramProjects
            .AsNoTracking()
            .Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(x => x.Category == category);

        var programs = await query
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        ViewBag.SelectedCategory = category;
        ViewBag.Categories = await _context.ProgramProjects
            .AsNoTracking()
            .Where(x => x.IsActive && x.Category != null && x.Category != "")
            .Select(x => x.Category)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return View("~/Areas/Portal/Views/DonorPrograms/Index.cshtml", programs);
    }

    [HttpGet("/Portal/Donor/Programs/Support/{id:int}")]
    public async Task<IActionResult> Support(int id)
    {
        var program = await _context.ProgramProjects
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        if (program == null)
            return NotFound();

        return View("~/Areas/Portal/Views/DonorPrograms/Support.cshtml", new DonorProgramSupportViewModel
        {
            ProgramProjectId = program.Id,
            Program = program,
            BankAccounts = await GetActiveBankAccountsAsync()
        });
    }

    [HttpPost("/Portal/Donor/Programs/Support/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Support(int id, DonorProgramSupportViewModel model)
    {
        var donorId = GetDonorId();
        var program = await _context.ProgramProjects.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        if (program == null)
            return NotFound();

        model.ProgramProjectId = id;
        model.Program = program;
        model.BankAccounts = await GetActiveBankAccountsAsync();

        if (!ModelState.IsValid)
            return View("~/Areas/Portal/Views/DonorPrograms/Support.cshtml", model);

        var contribution = new DonorContribution
        {
            ContributionCode = DonorContributionCodeGenerator.Generate(_context),
            DonorAccountId = donorId,
            ProgramProjectId = program.Id,
            Title = $"دعم - {program.Title}",
            Status = "بانتظار الاعتماد",
            ProgressPercent = 0,
            TotalAmount = model.Amount,
            SpentAmount = 0,
            RemainingAmount = model.Amount,
            TransactionNumber = model.TransactionNumber.Trim(),
            BeneficiariesCount = 0,
            ImpactSummary = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
            HasSurplus = false,
            IsSurplusLocked = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.DonorContributions.Add(contribution);
        await _context.SaveChangesAsync();

        _context.DonorContributionUpdates.Add(new DonorContributionUpdate
        {
            DonorContributionId = contribution.Id,
            Title = "تم إنشاء طلب الدعم",
            Details = "تم تسجيل رغبتك في دعم البرنامج، وسيتم اعتماد المساهمة وتوجيهها من قبل الجمعية.",
            ProgressPercent = 0,
            CreatedAt = DateTime.Now
        });

        _context.DonorNotifications.Add(new DonorNotification
        {
            DonorAccountId = donorId,
            DonorContributionId = contribution.Id,
            Title = "تم استلام طلب الدعم",
            Message = $"تم استلام دعم برنامج {program.Title} بقيمة {model.Amount:N2} ر.س، رقم العملية: {model.TransactionNumber.Trim()}، رقم الطلب: {contribution.ContributionCode}، وسيتم إشعارك بعد الاعتماد.",
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تسجيل طلب الدعم بنجاح، وسيظهر الآن ضمن مساهماتك.";
        return Redirect($"/Portal/Donor/Contributions/Details/{contribution.Id}");
    }

    private async Task<IReadOnlyList<Kafo.Web.Models.BankAccount>> GetActiveBankAccountsAsync()
    {
        return await _context.BankAccounts
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.BankName)
            .ToListAsync();
    }

    private int GetDonorId()
    {
        var value = User.FindFirstValue("KafoDonorUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var donorId) ? donorId : 0;
    }
}


