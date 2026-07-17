using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/Pages")]
public class PagesExecutiveManagerController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILogger<PagesExecutiveManagerController> _logger;

    public PagesExecutiveManagerController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILogger<PagesExecutiveManagerController> logger)
    {
        _context = context;
        _files = files;
        _logger = logger;
    }

    [HttpGet("ExecutiveManager")]
    public async Task<IActionResult> ExecutiveManager()
    {
        var page = await GetOrCreateExecutiveManagerMessageAsync();
        return View("~/Areas/Admin/Views/Pages/ExecutiveManager.cshtml", page);
    }

    [HttpPost("ExecutiveManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecutiveManager(ExecutiveManagerMessage model, IFormFile? imageFile, IFormFile? signatureFile)
    {
        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/Pages/ExecutiveManager.cshtml", model);

        try
        {
            var page = await _context.ExecutiveManagerMessages.FirstOrDefaultAsync();

            if (page == null)
            {
                page = new ExecutiveManagerMessage
                {
                    CreatedAt = DateTime.Now
                };

                _context.ExecutiveManagerMessages.Add(page);
            }

            if (imageFile != null)
            {
                _files.Delete(page.ImagePath);
                page.ImagePath = await _files.UploadAsync(imageFile, "executive-manager");
            }

            if (signatureFile != null)
            {
                _files.Delete(page.SignatureImagePath);
                page.SignatureImagePath = await _files.UploadAsync(signatureFile, "executive-manager-signatures");
            }

            page.ManagerName = model.ManagerName.Trim();
            page.PositionTitle = model.PositionTitle.Trim();
            page.Title = model.Title.Trim();
            page.MessageText = model.MessageText.Trim();
            page.IsActive = model.IsActive;
            page.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حفظ كلمة المدير التنفيذي بنجاح.";
            return RedirectToAction(nameof(ExecutiveManager));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while saving executive manager message");
            ModelState.AddModelError(string.Empty, "حدث خطأ أثناء حفظ كلمة المدير التنفيذي.");
            return View("~/Areas/Admin/Views/Pages/ExecutiveManager.cshtml", model);
        }
    }

    private async Task<ExecutiveManagerMessage> GetOrCreateExecutiveManagerMessageAsync()
    {
        var page = await _context.ExecutiveManagerMessages.FirstOrDefaultAsync();

        if (page != null)
            return page;

        page = new ExecutiveManagerMessage
        {
            ManagerName = "اسم المدير التنفيذي",
            PositionTitle = "المدير التنفيذي",
            Title = "كلمة المدير التنفيذي",
            MessageText = "نرحب بكم في جمعية كفؤ لتمكين ذوي الإعاقة بحائل، ونعمل مع فريق الجمعية على تطوير البرامج والمبادرات وتحسين الخدمات المقدمة للمستفيدين، بما يعزز جودة الحياة ويدعم التمكين والمشاركة الفاعلة في المجتمع.",
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.ExecutiveManagerMessages.Add(page);
        await _context.SaveChangesAsync();

        return page;
    }
}
