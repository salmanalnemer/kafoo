using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/Pages")]
public class PagesPresidentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILogger<PagesPresidentController> _logger;

    public PagesPresidentController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILogger<PagesPresidentController> logger)
    {
        _context = context;
        _files = files;
        _logger = logger;
    }

    [HttpGet("President")]
    public async Task<IActionResult> President()
    {
        var page = await GetOrCreatePresidentMessageAsync();
        return View("~/Areas/Admin/Views/Pages/President.cshtml", page);
    }

    [HttpPost("President")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> President(PresidentMessage model, IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/Pages/President.cshtml", model);

        try
        {
            var page = await _context.PresidentMessages.FirstOrDefaultAsync();

            if (page == null)
            {
                page = new PresidentMessage
                {
                    CreatedAt = DateTime.Now
                };

                _context.PresidentMessages.Add(page);
            }

            if (imageFile != null)
            {
                _files.Delete(page.ImagePath);
                page.ImagePath = await _files.UploadAsync(imageFile, "president");
            }

            page.LeaderName = model.LeaderName.Trim();
            page.PositionTitle = model.PositionTitle.Trim();
            page.Title = model.Title.Trim();
            page.MessageText = model.MessageText.Trim();
            page.IsActive = model.IsActive;
            page.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حفظ كلمة رئيس الجمعية بنجاح.";
            return RedirectToAction(nameof(President));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while saving president message");
            ModelState.AddModelError(string.Empty, "حدث خطأ أثناء حفظ كلمة رئيس الجمعية.");
            return View("~/Areas/Admin/Views/Pages/President.cshtml", model);
        }
    }

    private async Task<PresidentMessage> GetOrCreatePresidentMessageAsync()
    {
        var page = await _context.PresidentMessages.FirstOrDefaultAsync();

        if (page != null)
            return page;

        page = new PresidentMessage
        {
            LeaderName = "اسم رئيس الجمعية",
            PositionTitle = "رئيس مجلس الإدارة",
            Title = "كلمة رئيس الجمعية",
            MessageText = "نرحب بكم في جمعية كفؤ لتمكين ذوي الإعاقة بحائل، ونسعى من خلال برامجنا ومبادراتنا إلى تحسين جودة حياة ذوي الإعاقة وتمكينهم من المشاركة الفاعلة في المجتمع وسوق العمل، بما يتوافق مع مستهدفات رؤية المملكة 2030.",
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.PresidentMessages.Add(page);
        await _context.SaveChangesAsync();

        return page;
    }
}
