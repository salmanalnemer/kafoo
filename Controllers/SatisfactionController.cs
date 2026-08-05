using Kafo.Web.Data;
using Kafo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[EnableRateLimiting("public-forms")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SatisfactionController : Controller
{
    private static readonly HashSet<string> AllowedBeneficiaryTypes =
        new(["مستفيد", "متبرع", "متطوع", "شريك", "زائر", "أخرى"], StringComparer.Ordinal);

    private static readonly HashSet<string> AllowedSatisfactionLevels =
        new(["راضٍ جداً", "راضٍ", "محايد", "غير راضٍ", "غير راضٍ جداً"], StringComparer.Ordinal);

    private readonly ApplicationDbContext _context;
    private readonly ILogger<SatisfactionController> _logger;

    public SatisfactionController(
        ApplicationDbContext context,
        ILogger<SatisfactionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("/Satisfaction")]
    public IActionResult Index()
        => View("~/Views/Satisfaction/Index.cshtml", new SatisfactionResponse
        {
            BeneficiaryType = "مستفيد",
            SatisfactionLevel = "راضٍ جداً",
            Rating = 5
        });

    [HttpPost("/Satisfaction")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(64 * 1024)]
    public async Task<IActionResult> Index(
        [Bind("FullName,Phone,Email,BeneficiaryType,ServiceName,SatisfactionLevel,Rating,PositiveNotes,ImprovementNotes,Suggestions")]
        SatisfactionResponse model,
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
        model.BeneficiaryType = string.IsNullOrWhiteSpace(model.BeneficiaryType)
            ? "مستفيد"
            : model.BeneficiaryType.Trim();
        model.ServiceName = (model.ServiceName ?? string.Empty).Trim();
        model.SatisfactionLevel = string.IsNullOrWhiteSpace(model.SatisfactionLevel)
            ? "راضٍ جداً"
            : model.SatisfactionLevel.Trim();
        model.PositiveNotes = NormalizeOptional(model.PositiveNotes);
        model.ImprovementNotes = NormalizeOptional(model.ImprovementNotes);
        model.Suggestions = NormalizeOptional(model.Suggestions);

        if (!AllowedBeneficiaryTypes.Contains(model.BeneficiaryType))
            ModelState.AddModelError(nameof(model.BeneficiaryType), "نوع المستفيد غير صالح.");

        if (!AllowedSatisfactionLevels.Contains(model.SatisfactionLevel))
            ModelState.AddModelError(nameof(model.SatisfactionLevel), "مستوى الرضا غير صالح.");

        if (model.Rating is < 1 or > 5)
            ModelState.AddModelError(nameof(model.Rating), "التقييم يجب أن يكون من 1 إلى 5.");

        if (string.IsNullOrWhiteSpace(model.ServiceName))
            ModelState.AddModelError(nameof(model.ServiceName), "اسم الخدمة مطلوب.");

        if (!ModelState.IsValid)
            return View("~/Views/Satisfaction/Index.cshtml", model);

        var entity = new SatisfactionResponse
        {
            FullName = model.FullName,
            Phone = model.Phone,
            Email = model.Email,
            BeneficiaryType = model.BeneficiaryType,
            ServiceName = model.ServiceName,
            SatisfactionLevel = model.SatisfactionLevel,
            Rating = model.Rating,
            PositiveNotes = model.PositiveNotes,
            ImprovementNotes = model.ImprovementNotes,
            Suggestions = model.Suggestions,
            IsRead = false,
            IsArchived = false,
            AdminNotes = null,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        try
        {
            _context.SatisfactionResponses.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Unable to save satisfaction response");
            ModelState.AddModelError(string.Empty, "تعذر حفظ التقييم حاليًا. حاول مرة أخرى.");
            return View("~/Views/Satisfaction/Index.cshtml", model);
        }

        TempData["Success"] = "شكراً لك، تم إرسال تقييم الرضا بنجاح.";
        return Redirect("/Satisfaction");
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
