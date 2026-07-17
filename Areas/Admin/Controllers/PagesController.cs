using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class PagesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILogger<PagesController> _logger;

    public PagesController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILogger<PagesController> logger)
    {
        _context = context;
        _files = files;
        _logger = logger;
    }

    public async Task<IActionResult> About()
    {
        var page = await GetOrCreateAboutPageAsync();
        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> About(SiteContentPage model, IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var page = await _context.SiteContentPages
                .FirstOrDefaultAsync(x => x.PageKey == "about");

            if (page == null)
            {
                page = new SiteContentPage
                {
                    PageKey = "about",
                    CreatedAt = DateTime.Now
                };

                _context.SiteContentPages.Add(page);
            }

            if (imageFile != null)
            {
                _files.Delete(page.ImagePath);
                page.ImagePath = await _files.UploadAsync(imageFile, "pages");
            }

            page.Title = model.Title.Trim();
            page.Subtitle = string.IsNullOrWhiteSpace(model.Subtitle) ? null : model.Subtitle.Trim();
            page.Content = model.Content.Trim();
            page.IsActive = model.IsActive;
            page.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حفظ صفحة من نحن بنجاح.";
            return RedirectToAction(nameof(About));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while saving about page");
            ModelState.AddModelError(string.Empty, "حدث خطأ أثناء حفظ صفحة من نحن.");
            return View(model);
        }
    }

    public async Task<IActionResult> Vision()
    {
        await SeedVisionCardsIfEmptyAsync();

        var model = new VisionMissionAdminViewModel
        {
            Form = new VisionMissionCard
            {
                Icon = "fa-solid fa-eye",
                IsActive = true
            },
            Cards = await GetVisionCardsAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVisionCard([Bind(Prefix = "Form")] VisionMissionCard form)
    {
        if (!ModelState.IsValid)
        {
            var model = new VisionMissionAdminViewModel
            {
                Form = form,
                Cards = await GetVisionCardsAsync()
            };

            return View("Vision", model);
        }

        form.Title = form.Title.Trim();
        form.Content = form.Content.Trim();
        form.Icon = string.IsNullOrWhiteSpace(form.Icon) ? "fa-solid fa-eye" : form.Icon.Trim();
        form.CreatedAt = DateTime.Now;
        form.UpdatedAt = DateTime.Now;

        _context.VisionMissionCards.Add(form);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تمت إضافة البطاقة بنجاح.";
        return RedirectToAction(nameof(Vision));
    }

    public async Task<IActionResult> EditVisionCard(int id)
    {
        var card = await _context.VisionMissionCards.FindAsync(id);

        if (card == null)
            return NotFound();

        return View(card);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditVisionCard(int id, VisionMissionCard model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var card = await _context.VisionMissionCards.FindAsync(id);

        if (card == null)
            return NotFound();

        card.Title = model.Title.Trim();
        card.Content = model.Content.Trim();
        card.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "fa-solid fa-eye" : model.Icon.Trim();
        card.DisplayOrder = model.DisplayOrder;
        card.IsActive = model.IsActive;
        card.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تعديل البطاقة بنجاح.";
        return RedirectToAction(nameof(Vision));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVisionCardStatus(int id)
    {
        var card = await _context.VisionMissionCards.FindAsync(id);

        if (card == null)
            return NotFound();

        card.IsActive = !card.IsActive;
        card.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Vision));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVisionCard(int id)
    {
        var card = await _context.VisionMissionCards.FindAsync(id);

        if (card == null)
            return NotFound();

        _context.VisionMissionCards.Remove(card);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف البطاقة بنجاح.";
        return RedirectToAction(nameof(Vision));
    }

    private async Task<IReadOnlyList<VisionMissionCard>> GetVisionCardsAsync()
    {
        return await _context.VisionMissionCards
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();
    }

    private async Task SeedVisionCardsIfEmptyAsync()
    {
        if (await _context.VisionMissionCards.AnyAsync())
            return;

        _context.VisionMissionCards.AddRange(
            new VisionMissionCard
            {
                Title = "رؤيتنا",
                Content = "أن نكون كيانًا رائدًا يساهم في تمكين الأشخاص من ذوي الإعاقة.",
                Icon = "fa-solid fa-eye",
                DisplayOrder = 1,
                IsActive = true
            },
            new VisionMissionCard
            {
                Title = "رسالتنا",
                Content = "تمكين الأشخاص من ذوي الإعاقة وزيادة فاعليتهم في المجتمع من خلال تعليمهم وتدريبهم وتمكينهم والمساهمة في توفير فرص عمل مناسبة لقدراتهم وإمكاناتهم.",
                Icon = "fa-solid fa-envelope",
                DisplayOrder = 2,
                IsActive = true
            },
            new VisionMissionCard
            {
                Title = "قيمنا",
                Content = "الشمولية، التكامل، الابتكار، المسؤولية المجتمعية.",
                Icon = "fa-solid fa-gem",
                DisplayOrder = 3,
                IsActive = true
            }
        );

        await _context.SaveChangesAsync();
    }

    private async Task<SiteContentPage> GetOrCreateAboutPageAsync()
    {
        var page = await _context.SiteContentPages
            .FirstOrDefaultAsync(x => x.PageKey == "about");

        if (page != null)
            return page;

        page = new SiteContentPage
        {
            PageKey = "about",
            Title = "من نحن",
            Subtitle = "جمعية كفؤ لتمكين ذوي الإعاقة بحائل",
            Content = "جمعية كفؤ لتمكين ذوي الإعاقة بحائل تسعى لتحسين جودة حياة ذوي الإعاقة عبر التدريب والتأهيل المهني، وزيادة وعي المجتمع، وتمكينهم من المشاركة الفاعلة في المجتمع وسوق العمل بما يتناسب مع قدراتهم وإمكاناتهم، انسجامًا مع رؤية المملكة 2030.",
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.SiteContentPages.Add(page);
        await _context.SaveChangesAsync();

        return page;
    }
}
