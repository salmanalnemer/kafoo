using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[EnableRateLimiting("public-forms")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("Contact")]
public sealed class ContactController : Controller
{
    private const long MaximumRequestBytes = 12L * 1024 * 1024;

    private static readonly HashSet<string> AllowedMessageTypes =
        new(["استفسار", "شكوى", "اقتراح", "طلب خدمة", "أخرى"], StringComparer.Ordinal);

    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILogger<ContactController> _logger;

    public ContactController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILogger<ContactController> logger)
    {
        _context = context;
        _files = files;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index()
        => View("~/Views/Contact/Index.cshtml", new ContactMessage());

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaximumRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumRequestBytes)]
    public async Task<IActionResult> Index(
        [Bind("FullName,Phone,Email,MessageType,Subject,MessageBody")]
        ContactMessage model,
        IFormFile? attachmentFile,
        string? website,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(website))
        {
            TempData["Success"] = "تم استلام الطلب.";
            return RedirectToAction(nameof(Index));
        }

        model.FullName = (model.FullName ?? string.Empty).Trim();
        model.Phone = (model.Phone ?? string.Empty).Trim();
        model.Email = NormalizeOptional(model.Email);
        model.MessageType = string.IsNullOrWhiteSpace(model.MessageType)
            ? "استفسار"
            : model.MessageType.Trim();
        model.Subject = (model.Subject ?? string.Empty).Trim();
        model.MessageBody = (model.MessageBody ?? string.Empty).Trim();

        if (!AllowedMessageTypes.Contains(model.MessageType))
            ModelState.AddModelError(nameof(model.MessageType), "نوع الرسالة غير صالح.");

        if (string.IsNullOrWhiteSpace(model.FullName))
            ModelState.AddModelError(nameof(model.FullName), "الاسم مطلوب.");

        if (string.IsNullOrWhiteSpace(model.Phone))
            ModelState.AddModelError(nameof(model.Phone), "رقم الجوال مطلوب.");

        if (string.IsNullOrWhiteSpace(model.Subject))
            ModelState.AddModelError(nameof(model.Subject), "عنوان الرسالة مطلوب.");

        if (string.IsNullOrWhiteSpace(model.MessageBody))
            ModelState.AddModelError(nameof(model.MessageBody), "نص الرسالة مطلوب.");

        if (!ModelState.IsValid)
            return View("~/Views/Contact/Index.cshtml", model);

        string? storedAttachment = null;

        try
        {
            if (attachmentFile is { Length: > 0 })
            {
                storedAttachment = await _files.UploadAsync(
                    attachmentFile,
                    "contact-messages",
                    cancellationToken);
            }

            var entity = new ContactMessage
            {
                FullName = model.FullName,
                Phone = model.Phone,
                Email = model.Email,
                MessageType = model.MessageType,
                Subject = model.Subject,
                MessageBody = model.MessageBody,
                AttachmentPath = storedAttachment,
                Status = "جديدة",
                AdminNotes = null,
                IsRead = false,
                IsArchived = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.ContactMessages.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _files.Delete(storedAttachment);
            _logger.LogWarning(ex, "Rejected contact form attachment");
            ModelState.AddModelError("attachmentFile", ex.Message);
            return View("~/Views/Contact/Index.cshtml", model);
        }
        catch (OperationCanceledException)
        {
            _files.Delete(storedAttachment);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _files.Delete(storedAttachment);
            _logger.LogError(ex, "Unable to save contact form submission");
            ModelState.AddModelError(string.Empty, "تعذر حفظ الرسالة حاليًا. حاول مرة أخرى.");
            return View("~/Views/Contact/Index.cshtml", model);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _files.Delete(storedAttachment);
            _logger.LogError(ex, "Unexpected failure while processing the public form");
            ModelState.AddModelError(string.Empty, "تعذر معالجة الطلب حاليًا. حاول مرة أخرى.");
            return View("~/Views/Contact/Index.cshtml", model);
        }

        TempData["Success"] = "تم إرسال رسالتك بنجاح. سيتم مراجعتها من قبل الفريق المختص.";
        return RedirectToAction(nameof(Index));
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
