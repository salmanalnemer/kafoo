using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kafo.Web.Controllers;

[EnableRateLimiting("public-forms")]
public class FeedbackController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public FeedbackController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    [HttpGet("/Feedback")]
    public IActionResult Index()
    {
        return View("~/Views/Feedback/Index.cshtml", new FeedbackEntry
        {
            FeedbackType = "ملاحظة",
            Rating = 5
        });
    }

    [HttpPost("/Feedback")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(FeedbackEntry model, IFormFile? attachmentFile, string? website)
    {
        if (!string.IsNullOrWhiteSpace(website))
        {
            // Honeypot field: return a normal response without storing automated spam.
            TempData["Success"] = "تم استلام الطلب.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.Remove(nameof(model.AttachmentPath));
        ModelState.Remove(nameof(model.Status));
        ModelState.Remove(nameof(model.AdminNotes));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Views/Feedback/Index.cshtml", model);

        if (attachmentFile != null && attachmentFile.Length > 0)
            model.AttachmentPath = await _files.UploadAsync(attachmentFile, "feedback-attachments");

        model.FullName = string.IsNullOrWhiteSpace(model.FullName) ? null : model.FullName.Trim();
        model.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        model.FeedbackType = string.IsNullOrWhiteSpace(model.FeedbackType) ? "ملاحظة" : model.FeedbackType.Trim();
        model.RelatedService = string.IsNullOrWhiteSpace(model.RelatedService) ? null : model.RelatedService.Trim();
        model.Subject = model.Subject.Trim();
        model.FeedbackBody = model.FeedbackBody.Trim();
        model.Status = "جديدة";
        model.IsRead = false;
        model.IsArchived = false;
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;

        _context.FeedbackEntries.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "شكراً لك، تم إرسال التغذية الراجعة بنجاح.";
        return Redirect("/Feedback");
    }
}
