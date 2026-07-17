using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[EnableRateLimiting("public-forms")]
[Route("InKindDonationRequest")]
public class InKindDonationRequestController : Controller
{
    private readonly ApplicationDbContext _context;

    public InKindDonationRequestController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        await SeedTypesIfEmptyAsync();

        var model = new InKindDonationRequestFormViewModel
        {
            DonationTypes = await GetActiveTypesAsync()
        };

        return View("~/Views/InKindDonationRequest/Index.cshtml", model);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(InKindDonationRequestFormViewModel model, string? website)
    {
        if (!string.IsNullOrWhiteSpace(website))
        {
            // Honeypot field: return a normal response without storing automated spam.
            TempData["Success"] = "تم استلام الطلب.";
            return RedirectToAction(nameof(Index));
        }

        await SeedTypesIfEmptyAsync();

        if (model.SelectedDonationTypeIds == null || !model.SelectedDonationTypeIds.Any())
            ModelState.AddModelError(nameof(model.SelectedDonationTypeIds), "يجب اختيار نوع تبرع واحد على الأقل.");

        if (!model.AcceptTerms)
            ModelState.AddModelError(nameof(model.AcceptTerms), "يجب تأكيد صحة البيانات والموافقة على التنبيه.");

        var selectedTypes = await _context.InKindDonationTypes
            .Where(x => x.IsActive && model.SelectedDonationTypeIds.Contains(x.Id))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        if (!selectedTypes.Any())
            ModelState.AddModelError(nameof(model.SelectedDonationTypeIds), "يجب اختيار نوع تبرع صحيح.");

        if (!ModelState.IsValid)
        {
            model.DonationTypes = await GetActiveTypesAsync();
            return View("~/Views/InKindDonationRequest/Index.cshtml", model);
        }

        var request = new InKindDonationRequest
        {
            DonorName = model.DonorName.Trim(),
            Mobile = NormalizeMobile(model.Mobile),
            City = model.City.Trim(),
            District = string.IsNullOrWhiteSpace(model.District) ? null : model.District.Trim(),
            DonationTypeIds = string.Join(",", selectedTypes.Select(x => x.Id)),
            DonationTypeNames = string.Join("، ", selectedTypes.Select(x => x.Name)),
            PreferredPickupTime = string.IsNullOrWhiteSpace(model.PreferredPickupTime) ? "morning" : model.PreferredPickupTime,
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
            Status = "جديد",
            CreatedAt = DateTime.Now
        };

        _context.InKindDonationRequests.Add(request);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إرسال طلب التبرع بنجاح، وسيتم التواصل معكم لتحديد موعد الاستلام.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<IReadOnlyList<InKindDonationType>> GetActiveTypesAsync()
    {
        return await _context.InKindDonationTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();
    }

    private async Task SeedTypesIfEmptyAsync()
    {
        if (await _context.InKindDonationTypes.AnyAsync())
            return;

        var names = new[]
        {
            "أجهزة كهربائية",
            "أثاث",
            "أدوات منزلية",
            "ملابس",
            "أحذية",
            "حقائب",
            "مفروشات",
            "بطانيات",
            "ألعاب أطفال",
            "كتب",
            "أدوات مدرسية",
            "أجهزة حاسب",
            "جوالات",
            "أجهزة طبية",
            "كرسي متحرك",
            "سرير طبي",
            "مواد غذائية",
            "أدوات نظافة",
            "مكيفات",
            "ثلاجات",
            "غسالات",
            "أواني منزلية",
            "أجهزة مطبخ",
            "أخرى"
        };

        var order = 1;

        foreach (var name in names)
        {
            _context.InKindDonationTypes.Add(new InKindDonationType
            {
                Name = name,
                Icon = "▣",
                DisplayOrder = order++,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
    }

    private static string NormalizeMobile(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return new string(value.Where(char.IsDigit).ToArray());
    }
}
