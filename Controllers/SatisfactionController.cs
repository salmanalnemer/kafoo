using Kafo.Web.Data;
using Kafo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kafo.Web.Controllers;

[EnableRateLimiting("public-forms")]
public class SatisfactionController : Controller
{
    private readonly ApplicationDbContext _context;

    public SatisfactionController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Satisfaction")]
    public IActionResult Index()
    {
        return View("~/Views/Satisfaction/Index.cshtml", new SatisfactionResponse
        {
            BeneficiaryType = "مستفيد",
            SatisfactionLevel = "راضٍ جداً",
            Rating = 5
        });
    }

    [HttpPost("/Satisfaction")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SatisfactionResponse model, string? website)
    {
        if (!string.IsNullOrWhiteSpace(website))
        {
            // Honeypot field: return a normal response without storing automated spam.
            TempData["Success"] = "تم استلام الطلب.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.Remove(nameof(model.AdminNotes));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.UpdatedAt));

        if (!ModelState.IsValid)
            return View("~/Views/Satisfaction/Index.cshtml", model);

        model.FullName = string.IsNullOrWhiteSpace(model.FullName) ? null : model.FullName.Trim();
        model.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        model.BeneficiaryType = string.IsNullOrWhiteSpace(model.BeneficiaryType) ? "مستفيد" : model.BeneficiaryType.Trim();
        model.ServiceName = model.ServiceName.Trim();
        model.SatisfactionLevel = string.IsNullOrWhiteSpace(model.SatisfactionLevel) ? "راضٍ جداً" : model.SatisfactionLevel.Trim();
        model.PositiveNotes = string.IsNullOrWhiteSpace(model.PositiveNotes) ? null : model.PositiveNotes.Trim();
        model.ImprovementNotes = string.IsNullOrWhiteSpace(model.ImprovementNotes) ? null : model.ImprovementNotes.Trim();
        model.Suggestions = string.IsNullOrWhiteSpace(model.Suggestions) ? null : model.Suggestions.Trim();
        model.IsRead = false;
        model.IsArchived = false;
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;

        _context.SatisfactionResponses.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "شكراً لك، تم إرسال تقييم الرضا بنجاح.";
        return Redirect("/Satisfaction");
    }
}
