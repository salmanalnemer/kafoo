using System.Text.Json;
using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/Pages")]
public class PagesOrganizationController : Controller
{
    private const int MaxLayoutNodes = 100;
    private const int MaxLayoutJsonLength = 60_000;
    private const int MaxImageBytes = 5 * 1024 * 1024;
    private const string PngPrefix = "data:image/png;base64,";

    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILogger<PagesOrganizationController> _logger;

    public PagesOrganizationController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILogger<PagesOrganizationController> logger)
    {
        _context = context;
        _files = files;
        _logger = logger;
    }

    [HttpGet("OrganizationalStructure")]
    public async Task<IActionResult> OrganizationalStructure()
    {
        var page = await GetOrCreatePageAsync();
        return View("~/Areas/Admin/Views/Pages/OrganizationalStructure.cshtml", page);
    }

    [HttpPost("OrganizationalStructure")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OrganizationalStructure(
        OrganizationalStructurePage model,
        string? layoutJson,
        string? imageBase64,
        CancellationToken cancellationToken)
    {
        ModelState.Remove(nameof(OrganizationalStructurePage.LayoutJson));
        ModelState.Remove(nameof(OrganizationalStructurePage.ImagePath));
        ModelState.Remove(nameof(OrganizationalStructurePage.CreatedAt));
        ModelState.Remove(nameof(OrganizationalStructurePage.UpdatedAt));

        string? normalizedLayout = null;
        try
        {
            normalizedLayout = NormalizeLayoutJson(layoutJson);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/Pages/OrganizationalStructure.cshtml", model);

        try
        {
            var page = await _context.OrganizationalStructurePages.FirstOrDefaultAsync(cancellationToken);

            if (page == null)
            {
                page = new OrganizationalStructurePage { CreatedAt = DateTime.Now };
                _context.OrganizationalStructurePages.Add(page);
            }

            page.Title = model.Title.Trim();
            page.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            page.LayoutJson = normalizedLayout;
            page.IsActive = model.IsActive;
            page.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(imageBase64))
            {
                var newPath = await SaveBase64ImageAsync(imageBase64, cancellationToken);
                _files.Delete(page.ImagePath);
                page.ImagePath = newPath;
            }

            await _context.SaveChangesAsync(cancellationToken);

            TempData["Success"] = "تم حفظ ونشر الهيكل التنظيمي بنجاح.";
            return RedirectToAction(nameof(OrganizationalStructure));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("~/Areas/Admin/Views/Pages/OrganizationalStructure.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while saving organizational structure");
            ModelState.AddModelError(string.Empty, "حدث خطأ أثناء حفظ الهيكل التنظيمي.");
            return View("~/Areas/Admin/Views/Pages/OrganizationalStructure.cshtml", model);
        }
    }

    private async Task<OrganizationalStructurePage> GetOrCreatePageAsync()
    {
        var page = await _context.OrganizationalStructurePages.FirstOrDefaultAsync();

        if (page != null)
            return page;

        page = new OrganizationalStructurePage
        {
            Title = "الهيكل التنظيمي",
            Description = "الهيكل التنظيمي المعتمد للجمعية.",
            LayoutJson = """[{"id":1,"title":"رئيس مجلس الإدارة","x":470,"y":30,"parentId":null},{"id":2,"title":"المدير التنفيذي","x":470,"y":170,"parentId":1},{"id":3,"title":"إدارة البرامج","x":170,"y":330,"parentId":2},{"id":4,"title":"إدارة العلاقات","x":470,"y":330,"parentId":2},{"id":5,"title":"الإدارة المالية","x":770,"y":330,"parentId":2}]""",
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.OrganizationalStructurePages.Add(page);
        await _context.SaveChangesAsync();
        return page;
    }

    private async Task<string> SaveBase64ImageAsync(
        string imageBase64,
        CancellationToken cancellationToken)
    {
        if (!imageBase64.StartsWith(PngPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("صيغة صورة الهيكل غير مسموحة. استخدم PNG فقط.");

        var encoded = imageBase64[PngPrefix.Length..];
        if (encoded.Length > ((MaxImageBytes + 2) / 3) * 4 + 16)
            throw new InvalidOperationException("حجم صورة الهيكل يتجاوز 5 ميجابايت.");

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(encoded);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("بيانات صورة الهيكل غير صالحة.");
        }

        if (bytes.Length == 0 || bytes.Length > MaxImageBytes)
            throw new InvalidOperationException("حجم صورة الهيكل غير صالح.");

        await using var stream = new MemoryStream(bytes, writable: false);
        var formFile = new FormFile(stream, 0, bytes.Length, "image", "organizational-structure.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        return await _files.UploadAsync(formFile, "organizational-structure", cancellationToken);
    }

    private static string NormalizeLayoutJson(string? layoutJson)
    {
        if (string.IsNullOrWhiteSpace(layoutJson))
            throw new InvalidOperationException("يجب إنشاء عناصر الهيكل التنظيمي أولاً.");
        if (layoutJson.Length > MaxLayoutJsonLength)
            throw new InvalidOperationException("بيانات الهيكل التنظيمي أكبر من الحد المسموح.");

        List<OrganizationNodeInput>? nodes;
        try
        {
            nodes = JsonSerializer.Deserialize<List<OrganizationNodeInput>>(
                layoutJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("تنسيق بيانات الهيكل التنظيمي غير صالح.");
        }

        if (nodes is null || nodes.Count == 0 || nodes.Count > MaxLayoutNodes)
            throw new InvalidOperationException($"يجب أن يحتوي الهيكل على عنصر واحد إلى {MaxLayoutNodes} عناصر.");

        var ids = new HashSet<int>();
        foreach (var node in nodes)
        {
            node.Title = node.Title?.Trim() ?? string.Empty;
            if (node.Id <= 0 || !ids.Add(node.Id))
                throw new InvalidOperationException("معرّفات عناصر الهيكل غير صالحة أو مكررة.");
            if (node.Title.Length is < 1 or > 120)
                throw new InvalidOperationException("عنوان كل عنصر مطلوب وبحد أقصى 120 حرفًا.");
            if (!double.IsFinite(node.X) || !double.IsFinite(node.Y) ||
                node.X is < -10_000 or > 10_000 || node.Y is < -10_000 or > 10_000)
                throw new InvalidOperationException("إحداثيات أحد عناصر الهيكل غير صالحة.");
        }

        if (nodes.Any(x => x.ParentId.HasValue && (!ids.Contains(x.ParentId.Value) || x.ParentId == x.Id)))
            throw new InvalidOperationException("ارتباطات عناصر الهيكل غير صالحة.");

        return JsonSerializer.Serialize(nodes);
    }

    private sealed class OrganizationNodeInput
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public int? ParentId { get; set; }
    }
}
