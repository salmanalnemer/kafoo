using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[EnableRateLimiting("public-forms")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class FeedbackController : Controller
{
    private const long MaximumRequestBytes = 12L * 1024 * 1024;

    private static readonly HashSet<string> AllowedFeedbackTypes =
        new(["ملاحظة", "اقتراح", "تحسين خدمة", "مشكلة", "أخرى"], StringComparer.Ordinal);

    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILogger<FeedbackController> logger)
    {
        _context = context;
        _files = files;
        _logger = logger;
    }

    [HttpGet("/Feedback")]
    public IActionResult Index()
        => View("~/Views/Feedback/Index.cshtml", new FeedbackEntry
        {
            FeedbackType = "ملاحظة",
            Rating = 5
        });

    [HttpPost("/Feedback")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaximumRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumRequestBytes)]
    public async Task<IActionResult> Index(
        [Bind("FullName,Phone,Email,FeedbackType,RelatedService,Subject,FeedbackBody,Rating")]
        FeedbackEntry model,
        IFormFile? attachmentFile,
        string? website,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(website))
        {
            TempData["Success"] = "تم استلام الطلب.";
            return RedirectToAction(nameof(Index));
        }

        model.FullName = NormalizeOptional(model.FullName);
        model.Phone = NormalizeOptional(model.Phone);
        model.Email = NormalizeOptional(model.Email);
        model.FeedbackType = string.IsNullOrWhiteSpace(model.FeedbackType)
            ? "ملاحظة"
            : model.FeedbackType.Trim();
        model.RelatedService = NormalizeOptional(model.RelatedService);
        model.Subject = (model.Subject ?? string.Empty).Trim();
        model.FeedbackBody = (model.FeedbackBody ?? string.Empty).Trim();

        if (!AllowedFeedbackTypes.Contains(model.FeedbackType))
            ModelState.AddModelError(nameof(model.FeedbackType), "نوع التغذية الراجعة غير صالح.");

        if (model.Rating is < 1 or > 5)
            ModelState.AddModelError(nameof(model.Rating), "التقييم يجب أن يكون من 1 إلى 5.");

        if (string.IsNullOrWhiteSpace(model.Subject))
            ModelState.AddModelError(nameof(model.Subject), "عنوان التغذية الراجعة مطلوب.");

        if (string.IsNullOrWhiteSpace(model.FeedbackBody))
            ModelState.AddModelError(nameof(model.FeedbackBody), "نص التغذية الراجعة مطلوب.");

        if (!ModelState.IsValid)
            return View("~/Views/Feedback/Index.cshtml", model);

        string? storedAttachment = null;

        try
        {
            if (attachmentFile is { Length: > 0 })
            {
                storedAttachment = await _files.UploadAsync(
                    attachmentFile,
                    "feedback-attachments",
                    cancellationToken);
            }

            var entity = new FeedbackEntry
            {
                FullName = model.FullName,
                Phone = model.Phone,
                Email = model.Email,
                FeedbackType = model.FeedbackType,
                RelatedService = model.RelatedService,
                Subject = model.Subject,
                FeedbackBody = model.FeedbackBody,
                Rating = model.Rating,
                AttachmentPath = storedAttachment,
                Status = "جديدة",
                AdminNotes = null,
                IsRead = false,
                IsArchived = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.FeedbackEntries.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _files.Delete(storedAttachment);
            _logger.LogWarning(ex, "Rejected feedback attachment");
            ModelState.AddModelError("attachmentFile", ex.Message);
            return View("~/Views/Feedback/Index.cshtml", model);
        }
        catch (OperationCanceledException)
        {
            _files.Delete(storedAttachment);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _files.Delete(storedAttachment);
            _logger.LogError(ex, "Unable to save feedback submission");
            ModelState.AddModelError(string.Empty, "تعذر حفظ التغذية الراجعة حاليًا. حاول مرة أخرى.");
            return View("~/Views/Feedback/Index.cshtml", model);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _files.Delete(storedAttachment);
            _logger.LogError(ex, "Unexpected failure while processing the public form");
            ModelState.AddModelError(string.Empty, "تعذر معالجة الطلب حاليًا. حاول مرة أخرى.");
            return View("~/Views/Feedback/Index.cshtml", model);
        }

        TempData["Success"] = "شكراً لك، تم إرسال التغذية الراجعة بنجاح.";
        return Redirect("/Feedback");
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
