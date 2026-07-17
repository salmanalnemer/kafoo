using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kafo.Web.Controllers;

[EnableRateLimiting("public-forms")]
[Route("JobApplications")]
public class JobApplicationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public JobApplicationsController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View("~/Views/JobApplications/Index.cshtml", new JobApplication());
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(JobApplication model, IFormFile? cvFile, IFormFile? attachmentFile, string? website)
    {
        if (!string.IsNullOrWhiteSpace(website))
        {
            // Honeypot field: return a normal response without storing automated spam.
            TempData["Success"] = "تم استلام الطلب.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.Remove(nameof(model.CvFilePath));
        ModelState.Remove(nameof(model.AttachmentFilePath));
        ModelState.Remove(nameof(model.Status));
        ModelState.Remove(nameof(model.AdminNotes));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (cvFile == null || cvFile.Length == 0)
            ModelState.AddModelError(nameof(model.CvFilePath), "السيرة الذاتية مطلوبة.");

        if (!ModelState.IsValid)
            return View("~/Views/JobApplications/Index.cshtml", model);

        model.CvFilePath = await _files.UploadAsync(cvFile!, "job-applications-cv");

        if (attachmentFile != null && attachmentFile.Length > 0)
            model.AttachmentFilePath = await _files.UploadAsync(attachmentFile, "job-applications-attachments");

        model.FullName = model.FullName.Trim();
        model.Phone = model.Phone.Trim();
        model.Email = model.Email.Trim();
        model.NationalId = string.IsNullOrWhiteSpace(model.NationalId) ? null : model.NationalId.Trim();
        model.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();
        model.DesiredJobTitle = string.IsNullOrWhiteSpace(model.DesiredJobTitle) ? null : model.DesiredJobTitle.Trim();
        model.Qualification = string.IsNullOrWhiteSpace(model.Qualification) ? null : model.Qualification.Trim();
        model.Specialty = string.IsNullOrWhiteSpace(model.Specialty) ? null : model.Specialty.Trim();
        model.CoverLetter = string.IsNullOrWhiteSpace(model.CoverLetter) ? null : model.CoverLetter.Trim();
        model.Status = "جديد";
        model.IsRead = false;
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;

        _context.JobApplications.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إرسال طلب التوظيف بنجاح. سيتم مراجعة الطلب من قبل الفريق المختص.";
        return RedirectToAction(nameof(Index));
    }
}
