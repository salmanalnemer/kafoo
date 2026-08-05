using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[EnableRateLimiting("public-forms")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("JobApplications")]
public sealed class JobApplicationsController : Controller
{
    private const long MaximumRequestBytes = 22L * 1024 * 1024;

    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILogger<JobApplicationsController> _logger;

    public JobApplicationsController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILogger<JobApplicationsController> logger)
    {
        _context = context;
        _files = files;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index()
        => View("~/Views/JobApplications/Index.cshtml", new JobApplication());

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaximumRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumRequestBytes)]
    public async Task<IActionResult> Index(
        [Bind("FullName,Phone,Email,NationalId,City,DesiredJobTitle,Qualification,Specialty,ExperienceYears,CoverLetter")]
        JobApplication model,
        IFormFile? cvFile,
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
        model.Email = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
        model.NationalId = NormalizeOptional(model.NationalId);
        model.City = NormalizeOptional(model.City);
        model.DesiredJobTitle = NormalizeOptional(model.DesiredJobTitle);
        model.Qualification = NormalizeOptional(model.Qualification);
        model.Specialty = NormalizeOptional(model.Specialty);
        model.CoverLetter = NormalizeOptional(model.CoverLetter);

        if (cvFile is not { Length: > 0 })
            ModelState.AddModelError("cvFile", "السيرة الذاتية مطلوبة.");

        if (model.ExperienceYears is < 0 or > 80)
            ModelState.AddModelError(nameof(model.ExperienceYears), "عدد سنوات الخبرة غير صحيح.");

        if (string.IsNullOrWhiteSpace(model.FullName))
            ModelState.AddModelError(nameof(model.FullName), "الاسم الكامل مطلوب.");

        if (string.IsNullOrWhiteSpace(model.Phone))
            ModelState.AddModelError(nameof(model.Phone), "رقم الجوال مطلوب.");

        if (string.IsNullOrWhiteSpace(model.Email))
            ModelState.AddModelError(nameof(model.Email), "البريد الإلكتروني مطلوب.");

        if (!ModelState.IsValid)
            return View("~/Views/JobApplications/Index.cshtml", model);

        string? storedCv = null;
        string? storedAttachment = null;

        try
        {
            storedCv = await _files.UploadAsync(
                cvFile!,
                "job-applications-cv",
                cancellationToken);

            if (attachmentFile is { Length: > 0 })
            {
                storedAttachment = await _files.UploadAsync(
                    attachmentFile,
                    "job-applications-attachments",
                    cancellationToken);
            }

            var entity = new JobApplication
            {
                FullName = model.FullName,
                Phone = model.Phone,
                Email = model.Email,
                NationalId = model.NationalId,
                City = model.City,
                DesiredJobTitle = model.DesiredJobTitle,
                Qualification = model.Qualification,
                Specialty = model.Specialty,
                ExperienceYears = model.ExperienceYears,
                CoverLetter = model.CoverLetter,
                CvFilePath = storedCv,
                AttachmentFilePath = storedAttachment,
                Status = "جديد",
                AdminNotes = null,
                IsRead = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.JobApplications.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _files.Delete(storedAttachment);
            _files.Delete(storedCv);
            _logger.LogWarning(ex, "Rejected job application upload");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("~/Views/JobApplications/Index.cshtml", model);
        }
        catch (OperationCanceledException)
        {
            _files.Delete(storedAttachment);
            _files.Delete(storedCv);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _files.Delete(storedAttachment);
            _files.Delete(storedCv);
            _logger.LogError(ex, "Unable to save job application");
            ModelState.AddModelError(string.Empty, "تعذر حفظ طلب التوظيف حاليًا. حاول مرة أخرى.");
            return View("~/Views/JobApplications/Index.cshtml", model);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _files.Delete(storedAttachment);
            _files.Delete(storedCv);
            _logger.LogError(ex, "Unexpected failure while processing the public form");
            ModelState.AddModelError(string.Empty, "تعذر معالجة الطلب حاليًا. حاول مرة أخرى.");
            return View("~/Views/JobApplications/Index.cshtml", model);
        }

        TempData["Success"] = "تم إرسال طلب التوظيف بنجاح. سيتم مراجعة الطلب من قبل الفريق المختص.";
        return RedirectToAction(nameof(Index));
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
