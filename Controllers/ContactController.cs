using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kafo.Web.Controllers;

[EnableRateLimiting("public-forms")]
[Route("Contact")]
public class ContactController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public ContactController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View("~/Views/Contact/Index.cshtml", new ContactMessage());
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ContactMessage model, IFormFile? attachmentFile, string? website)
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
            return View("~/Views/Contact/Index.cshtml", model);

        if (attachmentFile != null && attachmentFile.Length > 0)
            model.AttachmentPath = await _files.UploadAsync(attachmentFile, "contact-messages");

        model.FullName = model.FullName.Trim();
        model.Phone = model.Phone.Trim();
        model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        model.MessageType = string.IsNullOrWhiteSpace(model.MessageType) ? "استفسار" : model.MessageType.Trim();
        model.Subject = model.Subject.Trim();
        model.MessageBody = model.MessageBody.Trim();
        model.Status = "جديدة";
        model.IsRead = false;
        model.IsArchived = false;
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;

        _context.ContactMessages.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إرسال رسالتك بنجاح. سيتم مراجعتها من قبل الفريق المختص.";
        return RedirectToAction(nameof(Index));
    }
}
