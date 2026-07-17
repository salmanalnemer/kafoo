using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/Pages")]
public class PagesNewBeneficiaryRegistrationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILogger<PagesNewBeneficiaryRegistrationController> _logger;

    public PagesNewBeneficiaryRegistrationController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILogger<PagesNewBeneficiaryRegistrationController> logger)
    {
        _context = context;
        _files = files;
        _logger = logger;
    }

    [HttpGet("NewBeneficiaryRegistration")]
    public async Task<IActionResult> NewBeneficiaryRegistration()
    {
        await SeedRequirementsIfEmptyAsync();

        var model = new NewBeneficiaryRegistrationAdminViewModel
        {
            Page = await GetOrCreatePageAsync(),
            Requirements = await GetRequirementsAsync(),
            NewRequirement = new NewBeneficiaryRegistrationRequirement
            {
                Icon = "▣",
                IsActive = true
            }
        };

        return View("~/Areas/Admin/Views/Pages/NewBeneficiaryRegistration.cshtml", model);
    }

    [HttpPost("NewBeneficiaryRegistration")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(150_000_000)]
    public async Task<IActionResult> NewBeneficiaryRegistration(IFormFile? videoFile)
    {
        try
        {
            var form = Request.Form;
            var page = await GetOrCreatePageAsync();

            var sourceType = FormValue(form, "Page.VideoSourceType");
            if (string.IsNullOrWhiteSpace(sourceType))
                sourceType = "Youtube";

            page.Title = Default(FormValue(form, "Page.Title"), "تسجيل مستفيد جديد");
            page.Subtitle = Default(FormValue(form, "Page.Subtitle"), "خدمة إلكترونية");
            page.Description = Default(FormValue(form, "Page.Description"), "يمكنك التسجيل كمستفيد جديد من خلال تعبئة البيانات وإرفاق المستندات المطلوبة.");

            page.VideoSourceType = sourceType;

            if (videoFile != null && videoFile.Length > 0)
            {
                page.VideoPath = await _files.UploadAsync(videoFile, "new-beneficiary-videos");
                page.VideoSourceType = "Upload";
            }

            page.YoutubeUrl = EmptyToNull(FormValue(form, "Page.YoutubeUrl"));

            page.RegisterButtonText = Default(FormValue(form, "Page.RegisterButtonText"), "التسجيل كمستفيد جديد");
            page.RegisterButtonUrl = NormalizeUrl(FormValue(form, "Page.RegisterButtonUrl"));

            page.AlertTitle = Default(FormValue(form, "Page.AlertTitle"), "تنبيه مهم");
            page.AlertText = Default(FormValue(form, "Page.AlertText"), "يشترط للتسجيل كمستفيد جديد تعبئة البيانات بدقة وإرفاق المستندات المطلوبة كاملة.");

            page.WhatsAppNumber = NormalizeWhatsAppNumber(FormValue(form, "Page.WhatsAppNumber"));
            page.WhatsAppButtonText = Default(FormValue(form, "Page.WhatsAppButtonText"), "طلب مساعدة عبر واتساب");
            page.WhatsAppMessage = Default(FormValue(form, "Page.WhatsAppMessage"), "السلام عليكم، أرغب بالتسجيل كمستفيد جديد وأحتاج المساعدة.");

            page.OpenRegisterInNewTab = FormBool(form, "Page.OpenRegisterInNewTab");
            page.ShowWhatsAppButton = FormBool(form, "Page.ShowWhatsAppButton");
            page.IsActive = FormBool(form, "Page.IsActive");

            page.UpdatedAt = DateTime.Now;

            var oldPages = await _context.NewBeneficiaryRegistrationPages
                .Where(x => x.Id != page.Id)
                .ToListAsync();

            foreach (var oldPage in oldPages)
            {
                oldPage.IsActive = false;
                oldPage.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حفظ إعداد الصفحة وربطها بالصفحة العامة بنجاح.";
            return RedirectToAction(nameof(NewBeneficiaryRegistration));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while saving new beneficiary registration page");
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(NewBeneficiaryRegistration));
        }
    }

    [HttpPost("NewBeneficiaryRegistration/AddRequirement")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRequirement([Bind(Prefix = "NewRequirement")] NewBeneficiaryRegistrationRequirement requirement)
    {
        ModelState.Remove("NewRequirement.CreatedAt");
        ModelState.Remove("NewRequirement.UpdatedAt");

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(requirement.Title))
        {
            TempData["Error"] = "تأكد من تعبئة اسم المستند.";
            return RedirectToAction(nameof(NewBeneficiaryRegistration));
        }

        requirement.Title = requirement.Title.Trim();
        requirement.Note = string.IsNullOrWhiteSpace(requirement.Note) ? null : requirement.Note.Trim();
        requirement.Icon = string.IsNullOrWhiteSpace(requirement.Icon) ? "▣" : requirement.Icon.Trim();
        requirement.CreatedAt = DateTime.Now;
        requirement.UpdatedAt = DateTime.Now;

        _context.NewBeneficiaryRegistrationRequirements.Add(requirement);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إضافة المستند المطلوب.";
        return RedirectToAction(nameof(NewBeneficiaryRegistration));
    }

    [HttpPost("NewBeneficiaryRegistration/ToggleRequirement/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRequirement(int id)
    {
        var item = await _context.NewBeneficiaryRegistrationRequirements.FindAsync(id);

        if (item == null)
            return NotFound();

        item.IsActive = !item.IsActive;
        item.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(NewBeneficiaryRegistration));
    }

    [HttpPost("NewBeneficiaryRegistration/DeleteRequirement/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRequirement(int id)
    {
        var item = await _context.NewBeneficiaryRegistrationRequirements.FindAsync(id);

        if (item == null)
            return NotFound();

        _context.NewBeneficiaryRegistrationRequirements.Remove(item);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف المستند المطلوب.";
        return RedirectToAction(nameof(NewBeneficiaryRegistration));
    }

    private async Task<NewBeneficiaryRegistrationPage> GetOrCreatePageAsync()
    {
        var page = await _context.NewBeneficiaryRegistrationPages
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (page != null)
            return page;

        page = new NewBeneficiaryRegistrationPage
        {
            Title = "تسجيل مستفيد جديد",
            Subtitle = "خدمة إلكترونية",
            Description = "يمكنك التسجيل كمستفيد جديد من خلال تعبئة البيانات وإرفاق المستندات المطلوبة.",
            VideoSourceType = "Youtube",
            RegisterButtonText = "التسجيل كمستفيد جديد",
            RegisterButtonUrl = "#",
            AlertTitle = "تنبيه مهم",
            AlertText = "يشترط للتسجيل كمستفيد جديد تعبئة البيانات بدقة وإرفاق المستندات المطلوبة كاملة.",
            WhatsAppNumber = "966500000000",
            WhatsAppButtonText = "طلب مساعدة عبر واتساب",
            WhatsAppMessage = "السلام عليكم، أرغب بالتسجيل كمستفيد جديد وأحتاج المساعدة.",
            OpenRegisterInNewTab = true,
            ShowWhatsAppButton = true,
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.NewBeneficiaryRegistrationPages.Add(page);
        await _context.SaveChangesAsync();

        return page;
    }

    private async Task<IReadOnlyList<NewBeneficiaryRegistrationRequirement>> GetRequirementsAsync()
    {
        return await _context.NewBeneficiaryRegistrationRequirements
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();
    }

    private async Task SeedRequirementsIfEmptyAsync()
    {
        if (await _context.NewBeneficiaryRegistrationRequirements.AnyAsync())
            return;

        _context.NewBeneficiaryRegistrationRequirements.AddRange(
            new NewBeneficiaryRegistrationRequirement { Title = "الهوية الوطنية", Note = "صورة واضحة سارية المفعول", Icon = "▣", DisplayOrder = 1, IsActive = true },
            new NewBeneficiaryRegistrationRequirement { Title = "التقرير الطبي", Note = "تقرير يثبت الحالة أو الإعاقة", Icon = "▣", DisplayOrder = 2, IsActive = true },
            new NewBeneficiaryRegistrationRequirement { Title = "مشهد الحالة الاجتماعية", Note = "عند الحاجة حسب نوع الطلب", Icon = "▣", DisplayOrder = 3, IsActive = true },
            new NewBeneficiaryRegistrationRequirement { Title = "بطاقة العائلة", Note = "لمن يلزمهم ذلك", Icon = "▣", DisplayOrder = 4, IsActive = true }
        );

        await _context.SaveChangesAsync();
    }

    private static string FormValue(IFormCollection form, string key)
    {
        return form.TryGetValue(key, out var value) ? value.ToString().Trim() : string.Empty;
    }

    private static bool FormBool(IFormCollection form, string key)
    {
        if (!form.TryGetValue(key, out var values))
            return false;

        return values.Any(x =>
            string.Equals(x, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x, "on", StringComparison.OrdinalIgnoreCase));
    }

    private static string Default(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "#";

        var url = value.Trim();

        if (url == "#" || url.StartsWith("/") ||
            url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
            return url;

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return "https://" + url;

        return url;
    }

    private static string? NormalizeWhatsAppNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("05"))
            digits = "966" + digits.Substring(1);

        if (digits.StartsWith("5") && digits.Length == 9)
            digits = "966" + digits;

        return digits;
    }
}
