using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/Pages")]
public class PagesBeneficiaryDataUpdateController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILogger<PagesBeneficiaryDataUpdateController> _logger;

    public PagesBeneficiaryDataUpdateController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILogger<PagesBeneficiaryDataUpdateController> logger)
    {
        _context = context;
        _files = files;
        _logger = logger;
    }

    [HttpGet("BeneficiaryDataUpdate")]
    public async Task<IActionResult> BeneficiaryDataUpdate()
    {
        await SeedRequirementsIfEmptyAsync();

        var model = new BeneficiaryDataUpdateAdminViewModel
        {
            Page = await GetOrCreatePageAsync(),
            Requirements = await GetRequirementsAsync(),
            NewRequirement = new BeneficiaryDataUpdateRequirement
            {
                Icon = "✓",
                IsActive = true
            }
        };

        return View("~/Areas/Admin/Views/Pages/BeneficiaryDataUpdate.cshtml", model);
    }

    [HttpPost("BeneficiaryDataUpdate")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(150_000_000)]
    public async Task<IActionResult> BeneficiaryDataUpdate([Bind(Prefix = "Page")] BeneficiaryDataUpdatePage model, IFormFile? videoFile)
    {
        ModelState.Remove("Page.VideoPath");
        ModelState.Remove("Page.CreatedAt");
        ModelState.Remove("Page.UpdatedAt");

        if (!ModelState.IsValid)
        {
            var invalidModel = new BeneficiaryDataUpdateAdminViewModel
            {
                Page = model,
                Requirements = await GetRequirementsAsync()
            };

            return View("~/Areas/Admin/Views/Pages/BeneficiaryDataUpdate.cshtml", invalidModel);
        }

        try
        {
            var page = await _context.BeneficiaryDataUpdatePages.FirstOrDefaultAsync();

            if (page == null)
            {
                page = new BeneficiaryDataUpdatePage
                {
                    CreatedAt = DateTime.Now
                };

                _context.BeneficiaryDataUpdatePages.Add(page);
            }

            if (videoFile != null && videoFile.Length > 0)
            {
                _files.Delete(page.VideoPath);
                page.VideoPath = await _files.UploadAsync(videoFile, "beneficiary-update-videos");
                page.VideoSourceType = "Upload";
            }
            else
            {
                page.VideoSourceType = string.IsNullOrWhiteSpace(model.VideoSourceType) ? "Youtube" : model.VideoSourceType;
            }

            page.Title = model.Title.Trim();
            page.Subtitle = string.IsNullOrWhiteSpace(model.Subtitle) ? "خدمة إلكترونية" : model.Subtitle.Trim();
            page.Description = model.Description.Trim();
            page.YoutubeUrl = string.IsNullOrWhiteSpace(model.YoutubeUrl) ? null : model.YoutubeUrl.Trim();

            page.AlertTitle = string.IsNullOrWhiteSpace(model.AlertTitle) ? "تنبيه مهم" : model.AlertTitle.Trim();
            page.AlertText = string.IsNullOrWhiteSpace(model.AlertText) ? "" : model.AlertText.Trim();

            page.PrimaryButtonText = string.IsNullOrWhiteSpace(model.PrimaryButtonText) ? "تحديث بياناتي الآن" : model.PrimaryButtonText.Trim();
            page.PrimaryButtonUrl = NormalizeUrl(model.PrimaryButtonUrl);

            page.SecondaryButtonText = string.IsNullOrWhiteSpace(model.SecondaryButtonText) ? "العودة للخدمات" : model.SecondaryButtonText.Trim();
            page.SecondaryButtonUrl = NormalizeUrl(model.SecondaryButtonUrl);

            page.OpenPrimaryInNewTab = model.OpenPrimaryInNewTab;

            page.WhatsAppNumber = NormalizeWhatsAppNumber(model.WhatsAppNumber);
            page.WhatsAppButtonText = string.IsNullOrWhiteSpace(model.WhatsAppButtonText) ? "تواصل عبر واتساب" : model.WhatsAppButtonText.Trim();
            page.WhatsAppMessage = string.IsNullOrWhiteSpace(model.WhatsAppMessage)
                ? "السلام عليكم، أرغب بالمساعدة في تحديث بيانات المستفيد."
                : model.WhatsAppMessage.Trim();

            page.ShowWhatsAppButton = model.ShowWhatsAppButton;
            page.IsActive = model.IsActive;
            page.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حفظ صفحة تحديث بيانات المستفيدين بنجاح.";
            return RedirectToAction(nameof(BeneficiaryDataUpdate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while saving beneficiary data update page");
            ModelState.AddModelError(string.Empty, ex.Message);

            var errorModel = new BeneficiaryDataUpdateAdminViewModel
            {
                Page = model,
                Requirements = await GetRequirementsAsync()
            };

            return View("~/Areas/Admin/Views/Pages/BeneficiaryDataUpdate.cshtml", errorModel);
        }
    }

    [HttpPost("BeneficiaryDataUpdate/AddRequirement")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRequirement([Bind(Prefix = "NewRequirement")] BeneficiaryDataUpdateRequirement requirement)
    {
        ModelState.Remove("NewRequirement.CreatedAt");
        ModelState.Remove("NewRequirement.UpdatedAt");

        if (!ModelState.IsValid)
            return RedirectToAction(nameof(BeneficiaryDataUpdate));

        requirement.Title = requirement.Title.Trim();
        requirement.Icon = string.IsNullOrWhiteSpace(requirement.Icon) ? "✓" : requirement.Icon.Trim();
        requirement.CreatedAt = DateTime.Now;
        requirement.UpdatedAt = DateTime.Now;

        _context.BeneficiaryDataUpdateRequirements.Add(requirement);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إضافة المستند المطلوب.";
        return RedirectToAction(nameof(BeneficiaryDataUpdate));
    }

    [HttpPost("BeneficiaryDataUpdate/ToggleRequirement/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRequirement(int id)
    {
        var item = await _context.BeneficiaryDataUpdateRequirements.FindAsync(id);

        if (item == null)
            return NotFound();

        item.IsActive = !item.IsActive;
        item.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(BeneficiaryDataUpdate));
    }

    [HttpPost("BeneficiaryDataUpdate/DeleteRequirement/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRequirement(int id)
    {
        var item = await _context.BeneficiaryDataUpdateRequirements.FindAsync(id);

        if (item == null)
            return NotFound();

        _context.BeneficiaryDataUpdateRequirements.Remove(item);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف المستند المطلوب.";
        return RedirectToAction(nameof(BeneficiaryDataUpdate));
    }

    private async Task<BeneficiaryDataUpdatePage> GetOrCreatePageAsync()
    {
        var page = await _context.BeneficiaryDataUpdatePages.FirstOrDefaultAsync();

        if (page != null)
            return page;

        page = new BeneficiaryDataUpdatePage
        {
            Title = "تحديث بيانات المستفيدين",
            Subtitle = "خدمة إلكترونية",
            Description = "يمكن للمستفيد تحديث بياناته وإرفاق المستندات المطلوبة من خلال الرابط المعتمد، مع مشاهدة شرح مبسط لطريقة تحديث البيانات.",
            YoutubeUrl = "",
            PrimaryButtonText = "تحديث بياناتي الآن",
            PrimaryButtonUrl = "/ServiceLinks",
            SecondaryButtonText = "العودة للخدمات",
            SecondaryButtonUrl = "/ServiceLinks",
            AlertTitle = "تنبيه مهم",
            AlertText = "يرجى التأكد من صحة البيانات وإرفاق المستندات المطلوبة قبل إرسال الطلب.",
            WhatsAppNumber = "966500000000",
            WhatsAppButtonText = "تواصل عبر واتساب",
            WhatsAppMessage = "السلام عليكم، أرغب بالمساعدة في تحديث بيانات المستفيد.",
            ShowWhatsAppButton = true,
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.BeneficiaryDataUpdatePages.Add(page);
        await _context.SaveChangesAsync();

        return page;
    }

    private async Task<IReadOnlyList<BeneficiaryDataUpdateRequirement>> GetRequirementsAsync()
    {
        return await _context.BeneficiaryDataUpdateRequirements
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();
    }

    private async Task SeedRequirementsIfEmptyAsync()
    {
        if (await _context.BeneficiaryDataUpdateRequirements.AnyAsync())
            return;

        _context.BeneficiaryDataUpdateRequirements.AddRange(
            new BeneficiaryDataUpdateRequirement { Title = "صورة الهوية الوطنية", Icon = "▣", DisplayOrder = 1, IsActive = true },
            new BeneficiaryDataUpdateRequirement { Title = "مشهد الإعاقة أو التقرير الطبي", Icon = "▣", DisplayOrder = 2, IsActive = true },
            new BeneficiaryDataUpdateRequirement { Title = "رقم الجوال والبريد الإلكتروني", Icon = "▣", DisplayOrder = 3, IsActive = true },
            new BeneficiaryDataUpdateRequirement { Title = "العنوان الوطني", Icon = "▣", DisplayOrder = 4, IsActive = true }
        );

        await _context.SaveChangesAsync();
    }

    private static string NormalizeUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "/ServiceLinks";

        var url = value.Trim();

        if (url.StartsWith("/") ||
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
            digits = "966" + digits[1..];

        if (digits.StartsWith("5") && digits.Length == 9)
            digits = "966" + digits;

        return digits;
    }
}
