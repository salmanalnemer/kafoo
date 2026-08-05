using Kafo.Web.Data;
using Kafo.Web.Security;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/JobApplications")]
[Authorize(AuthenticationSchemes = KafoAuthSchemes.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class JobApplicationsController : Controller
{
    private static readonly HashSet<string> AllowedStatuses =
        new(["جديد", "قيد المراجعة", "مقبول مبدئياً", "مرفوض"], StringComparer.Ordinal);

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
    public async Task<IActionResult> Index(
        string? q,
        string? status,
        CancellationToken cancellationToken)
    {
        q = NormalizeAndLimit(q, 120);
        status = NormalizeAndLimit(status, 80);

        var query = _context.JobApplications.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(x =>
                x.FullName.Contains(q) ||
                x.Phone.Contains(q) ||
                x.Email.Contains(q) ||
                (x.NationalId != null && x.NationalId.Contains(q)) ||
                (x.City != null && x.City.Contains(q)) ||
                (x.DesiredJobTitle != null && x.DesiredJobTitle.Contains(q)));
        }

        if (!string.IsNullOrWhiteSpace(status) && AllowedStatuses.Contains(status))
            query = query.Where(x => x.Status == status);
        else if (!string.IsNullOrWhiteSpace(status))
            status = null;

        var applications = await query
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        ViewBag.Search = q ?? string.Empty;
        ViewBag.Status = status ?? string.Empty;

        return View("~/Areas/Admin/Views/JobApplications/Index.cshtml", applications);
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var application = await _context.JobApplications
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (application == null)
            return NotFound();

        if (!application.IsRead)
        {
            application.IsRead = true;
            application.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return View("~/Areas/Admin/Views/JobApplications/Details.cshtml", application);
    }

    [HttpPost("UpdateStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> UpdateStatus(
        int id,
        string? status,
        string? adminNotes,
        CancellationToken cancellationToken)
    {
        var normalizedStatus = (status ?? string.Empty).Trim();
        if (!AllowedStatuses.Contains(normalizedStatus))
        {
            TempData["Error"] = "حالة طلب التوظيف غير صحيحة.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var normalizedNotes = string.IsNullOrWhiteSpace(adminNotes) ? null : adminNotes.Trim();
        if (normalizedNotes is { Length: > 1500 })
        {
            TempData["Error"] = "الملاحظات الإدارية تتجاوز الحد المسموح.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var application = await _context.JobApplications
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (application == null)
            return NotFound();

        application.Status = normalizedStatus;
        application.AdminNotes = normalizedNotes;
        application.IsRead = true;
        application.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "تم تحديث حالة طلب التوظيف.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var application = await _context.JobApplications
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (application == null)
            return NotFound();

        var cvPath = application.CvFilePath;
        var attachmentPath = application.AttachmentFilePath;

        _context.JobApplications.Remove(application);
        await _context.SaveChangesAsync(cancellationToken);

        // لا نحذف الملفات قبل نجاح حذف السجل حتى لا نخسر الملف عند فشل قاعدة البيانات.
        _files.Delete(attachmentPath);
        _files.Delete(cvPath);

        _logger.LogInformation("Deleted job application {ApplicationId} and its managed files", id);
        TempData["Success"] = "تم حذف طلب التوظيف ومرفقاته بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    private static string? NormalizeAndLimit(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength];
    }
}
