using Kafo.Web.Data;
using Kafo.Web.Models.Organizations;
using Kafo.Web.Security;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AdminOrganizationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly IPasswordSetupService _passwordSetup;
    private readonly ILogger<AdminOrganizationsController> _logger;

    private const long MaxCandidateCvSize = 10 * 1024 * 1024;

    private static readonly string[] AllowedCandidateCvExtensions =
    {
        ".pdf", ".doc", ".docx"
    };

    private static readonly string[] RequestStatuses =
    {
        "جديد", "قيد المراجعة", "قيد الترشيح", "تم ترشيح المرشحين",
        "تم إجراء المقابلات", "تم التوظيف أو القبول", "تم إغلاق الطلب"
    };

    private static readonly string[] CandidateStatuses =
    {
        "مرشح جديد", "تم التواصل", "تمت المقابلة", "تم القبول", "لم يتم القبول"
    };

    public AdminOrganizationsController(
        ApplicationDbContext context,
        IFileUploadService files,
        IPasswordSetupService passwordSetup,
        ILogger<AdminOrganizationsController> logger)
    {
        _context = context;
        _files = files;
        _passwordSetup = passwordSetup;
        _logger = logger;
    }

    [HttpGet("/Admin/Organizations")]
    public async Task<IActionResult> Index(string? q, string? status)
    {
        q = q?.Trim();
        status = status?.Trim();

        var query = _context.OrganizationAccounts.AsNoTracking()
            .Include(x => x.OpportunityRequests).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Name.Contains(q) ||
                (x.Activity != null && x.Activity.Contains(q)) ||
                (x.City != null && x.City.Contains(q)) ||
                (x.Email != null && x.Email.Contains(q)) ||
                (x.Phone != null && x.Phone.Contains(q)));

        if (status == "active") query = query.Where(x => x.IsActive);
        if (status == "disabled") query = query.Where(x => !x.IsActive);

        var model = new AdminOrganizationsIndexViewModel
        {
            Search = q ?? string.Empty,
            StatusFilter = status ?? string.Empty,
            Organizations = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(),
            PendingRequests = await _context.OpportunityRequests.AsNoTracking()
                .Include(x => x.OrganizationAccount)
                .Where(x => x.Status == "جديد" || x.Status == "قيد المراجعة")
                .OrderByDescending(x => x.CreatedAt).Take(15).ToListAsync(),
            TotalOrganizations = await _context.OrganizationAccounts.CountAsync(),
            ActiveOrganizations = await _context.OrganizationAccounts.CountAsync(x => x.IsActive),
            TotalRequests = await _context.OpportunityRequests.CountAsync(),
            PendingRequestsCount = await _context.OpportunityRequests.CountAsync(x => x.Status == "جديد" || x.Status == "قيد المراجعة"),
            TotalCandidates = await _context.OpportunityCandidates.CountAsync(),
            AcceptedCandidates = await _context.OpportunityCandidates.CountAsync(x => x.Status == "تم القبول")
        };
        return View("~/Areas/Admin/Views/Organizations/Index.cshtml", model);
    }

    [HttpGet("/Admin/Organizations/Create")]
    public IActionResult Create() => View("~/Areas/Admin/Views/Organizations/Create.cshtml", new AdminOrganizationFormViewModel());

    [HttpPost("/Admin/Organizations/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminOrganizationFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Email))
            ModelState.AddModelError(nameof(model.Email), "البريد الإلكتروني مطلوب لتسجيل الدخول وإرسال رمز التحقق.");
        else if (!PortalEmailPolicy.IsDeliverable(model.Email))
            ModelState.AddModelError(nameof(model.Email), "أدخل بريدًا إلكترونيًا حقيقيًا يمكنه استقبال رمز OTP.");
        ModelState.Remove(nameof(model.Password));
        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var normalizedEmail = model.Email.Trim().ToLowerInvariant();
            var emailExists = await _context.OrganizationAccounts.AnyAsync(x =>
                    x.Email != null && x.Email.ToLower() == normalizedEmail) ||
                await _context.DonorAccounts.AnyAsync(x =>
                    x.Email != null && x.Email.ToLower() == normalizedEmail);
            if (emailExists)
                ModelState.AddModelError(nameof(model.Email), "البريد الإلكتروني مستخدم في حساب داعم أو جهة أخرى.");
        }
        if (!ModelState.IsValid) return View("~/Areas/Admin/Views/Organizations/Create.cshtml", model);

        var initialSecret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
        var hashed = AdminPasswordHasher.HashPassword(initialSecret);
        var organization = new OrganizationAccount
        {
            Name = model.Name.Trim(), LogoPath = model.LogoPath?.Trim(), Activity = model.Activity?.Trim(),
            City = model.City?.Trim(), ContactName = model.ContactName?.Trim(), Email = model.Email.Trim().ToLowerInvariant(),
            Phone = model.Phone?.Trim(), PasswordHash = hashed.Hash, PasswordSalt = hashed.Salt,
            SecurityStamp = LoginSecurity.NewSecurityStamp(), MustChangePassword = true,
            IsActive = model.IsActive, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now
        };
        _context.OrganizationAccounts.Add(organization);
        await _context.SaveChangesAsync();
        TempData["Success"] = "تم إنشاء حساب الجهة بنجاح.";

        try
        {
            await _passwordSetup.IssueAsync(
                "Organization",
                organization.Id,
                organization.Email!,
                organization.Name,
                "بوابة الجهات والشركات",
                GetCurrentAdminId(),
                HttpContext);
            TempData["Success"] = "تم إنشاء حساب الجهة وإرسال رابط آمن لإعداد كلمة المرور.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to send new organization account credentials for organization {OrganizationId}", organization.Id);
            TempData["Error"] = "تم إنشاء الحساب، لكن تعذر إرسال رابط إعداد كلمة المرور. يمكنك إعادة إرساله من الإعدادات العامة.";
        }

        return RedirectToAction(nameof(Details), new { id = organization.Id });
    }

    [HttpGet("/Admin/Organizations/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.OrganizationAccounts.FindAsync(id);
        if (item == null) return NotFound();
        return View("~/Areas/Admin/Views/Organizations/Edit.cshtml", new AdminOrganizationFormViewModel
        {
            Id = item.Id, Name = item.Name, LogoPath = item.LogoPath, Activity = item.Activity, City = item.City,
            ContactName = item.ContactName, Email = item.Email ?? string.Empty, Phone = item.Phone, IsActive = item.IsActive
        });
    }

    [HttpPost("/Admin/Organizations/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminOrganizationFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var item = await _context.OrganizationAccounts.FindAsync(id);
        if (item == null) return NotFound();
        if (string.IsNullOrWhiteSpace(model.Email))
            ModelState.AddModelError(nameof(model.Email), "البريد الإلكتروني مطلوب لتسجيل الدخول وإرسال رمز التحقق.");
        else if (!PortalEmailPolicy.IsDeliverable(model.Email))
            ModelState.AddModelError(nameof(model.Email), "أدخل بريدًا إلكترونيًا حقيقيًا يمكنه استقبال رمز OTP.");
        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var normalizedEmail = model.Email.Trim().ToLowerInvariant();
            var emailExists = await _context.OrganizationAccounts.AnyAsync(x =>
                    x.Id != id && x.Email != null && x.Email.ToLower() == normalizedEmail) ||
                await _context.DonorAccounts.AnyAsync(x =>
                    x.Email != null && x.Email.ToLower() == normalizedEmail);
            if (emailExists)
                ModelState.AddModelError(nameof(model.Email), "البريد الإلكتروني مستخدم في حساب داعم أو جهة أخرى.");
        }
        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            foreach (var error in PasswordPolicy.Validate(model.Password))
                ModelState.AddModelError(nameof(model.Password), error);
        }
        if (!ModelState.IsValid) return View("~/Areas/Admin/Views/Organizations/Edit.cshtml", model);

        item.Name = model.Name.Trim(); item.LogoPath = model.LogoPath?.Trim(); item.Activity = model.Activity?.Trim();
        item.City = model.City?.Trim(); item.ContactName = model.ContactName?.Trim(); item.Email = model.Email.Trim().ToLowerInvariant();
        item.Phone = model.Phone?.Trim(); item.IsActive = model.IsActive; item.UpdatedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var hashed = AdminPasswordHasher.HashPassword(model.Password);
            item.PasswordHash = hashed.Hash; item.PasswordSalt = hashed.Salt;
            item.SecurityStamp = LoginSecurity.NewSecurityStamp(); item.MustChangePassword = false;
            item.AccessFailedCount = 0; item.LockoutEndUtc = null; item.PasswordChangedAtUtc = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
        TempData["Success"] = "تم تحديث بيانات الجهة.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("/Admin/Organizations/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var org = await _context.OrganizationAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (org == null) return NotFound();
        var requests = await _context.OpportunityRequests.AsNoTracking().Include(x => x.Candidates)
            .Where(x => x.OrganizationAccountId == id).OrderByDescending(x => x.CreatedAt).ToListAsync();
        var candidates = requests.SelectMany(x => x.Candidates).OrderByDescending(x => x.CreatedAt).ToList();
        var accepted = candidates.Count(x => x.Status == "تم القبول");
        var model = new AdminOrganizationDetailsViewModel
        {
            Organization = org, Requests = requests, Candidates = candidates,
            Notifications = await _context.OrganizationNotifications.AsNoTracking().Where(x => x.OrganizationAccountId == id).OrderByDescending(x => x.CreatedAt).Take(25).ToListAsync(),
            Evaluations = await _context.OrganizationEvaluations.AsNoTracking().Include(x => x.OpportunityRequest).Where(x => x.OrganizationAccountId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(),
            TotalOpportunities = requests.Sum(x => x.AvailableCount), AcceptedCandidates = accepted,
            SuccessRate = candidates.Count == 0 ? 0 : Math.Round(accepted * 100d / candidates.Count, 1)
        };
        return View("~/Areas/Admin/Views/Organizations/Details.cshtml", model);
    }

    [HttpGet("/Admin/Organizations/Requests/{id:int}")]
    public async Task<IActionResult> RequestDetails(int id)
    {
        var request = await _context.OpportunityRequests.AsNoTracking().Include(x => x.OrganizationAccount)
            .Include(x => x.Candidates.OrderByDescending(c => c.CreatedAt)).FirstOrDefaultAsync(x => x.Id == id);
        if (request == null) return NotFound();
        ViewBag.RequestStatuses = RequestStatuses; ViewBag.CandidateStatuses = CandidateStatuses;
        return View("~/Areas/Admin/Views/Organizations/RequestDetails.cshtml", request);
    }

    [HttpPost("/Admin/Organizations/Requests/{id:int}/Status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRequestStatus(int id, string status, string? message)
    {
        var request = await _context.OpportunityRequests.Include(x => x.OrganizationAccount).FirstOrDefaultAsync(x => x.Id == id);
        if (request == null) return NotFound();
        if (!RequestStatuses.Contains(status)) { TempData["Error"] = "حالة الطلب غير صحيحة."; return RedirectToAction(nameof(RequestDetails), new { id }); }
        request.Status = status; request.UpdatedAt = DateTime.Now;
        _context.OrganizationNotifications.Add(new OrganizationNotification
        {
            OrganizationAccountId = request.OrganizationAccountId, OpportunityRequestId = request.Id,
            Title = $"تحديث حالة طلب: {request.Title}", Message = string.IsNullOrWhiteSpace(message) ? $"تم تحديث حالة الطلب إلى: {status}." : message.Trim(),
            IsRead = false, CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();
        TempData["Success"] = "تم تحديث حالة الطلب وإشعار الجهة.";
        return RedirectToAction(nameof(RequestDetails), new { id });
    }

    [HttpPost("/Admin/Organizations/Requests/{id:int}/Candidates/Add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCandidate(
        int id,
        string candidateName,
        IFormFile? cvFile,
        string? qualifications,
        string? skills)
    {
        var request = await _context.OpportunityRequests.FindAsync(id);

        if (request == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(candidateName))
        {
            TempData["Error"] = "اسم المرشح مطلوب.";
            return RedirectToAction(nameof(RequestDetails), new { id });
        }

        string? cvFilePath = null;

        if (cvFile is { Length: > 0 })
        {
            var extension = Path.GetExtension(cvFile.FileName);

            if (!AllowedCandidateCvExtensions.Contains(
                    extension,
                    StringComparer.OrdinalIgnoreCase))
            {
                TempData["Error"] = "صيغة السيرة الذاتية غير مسموحة. الصيغ المتاحة: PDF أو DOC أو DOCX.";
                return RedirectToAction(nameof(RequestDetails), new { id });
            }

            if (cvFile.Length > MaxCandidateCvSize)
            {
                TempData["Error"] = "حجم السيرة الذاتية يتجاوز الحد المسموح وهو 10 ميجابايت.";
                return RedirectToAction(nameof(RequestDetails), new { id });
            }

            try
            {
                cvFilePath = await _files.UploadAsync(
                    cvFile,
                    "organization-candidate-cv");
            }
            catch (InvalidOperationException)
            {
                TempData["Error"] = "تعذر رفع السيرة الذاتية. تأكد من نوع الملف وحجمه ثم أعد المحاولة.";
                return RedirectToAction(nameof(RequestDetails), new { id });
            }
        }

        var candidate = new OpportunityCandidate
        {
            OpportunityRequestId = id,
            CandidateName = candidateName.Trim(),
            CvFilePath = cvFilePath,
            Qualifications = qualifications?.Trim(),
            Skills = skills?.Trim(),
            Status = "مرشح جديد",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.OpportunityCandidates.Add(candidate);

        request.Status = "تم ترشيح المرشحين";
        request.UpdatedAt = DateTime.Now;

        _context.OrganizationNotifications.Add(new OrganizationNotification
        {
            OrganizationAccountId = request.OrganizationAccountId,
            OpportunityRequestId = id,
            Title = "تمت إضافة مرشح جديد",
            Message = $"تم ترشيح {candidateName.Trim()} لطلب {request.Title}.",
            IsRead = false,
            CreatedAt = DateTime.Now
        });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            _files.Delete(cvFilePath);
            throw;
        }

        TempData["Success"] = "تمت إضافة المرشح ورفع السيرة الذاتية وإشعار الجهة.";
        return RedirectToAction(nameof(RequestDetails), new { id });
    }

    [HttpPost("/Admin/Organizations/Candidates/{candidateId:int}/Update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCandidate(int candidateId, string status, string? organizationNotes)
    {
        var candidate = await _context.OpportunityCandidates.Include(x => x.OpportunityRequest).FirstOrDefaultAsync(x => x.Id == candidateId);
        if (candidate == null) return NotFound();
        if (!CandidateStatuses.Contains(status)) return BadRequest();
        candidate.Status = status; candidate.OrganizationNotes = organizationNotes?.Trim(); candidate.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["Success"] = "تم تحديث حالة المرشح.";
        return RedirectToAction(nameof(RequestDetails), new { id = candidate.OpportunityRequestId });
    }

    [HttpPost("/Admin/Organizations/{id:int}/Notify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Notify(int id, string title, string message)
    {
        if (!await _context.OrganizationAccounts.AnyAsync(x => x.Id == id)) return NotFound();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message)) { TempData["Error"] = "عنوان ونص الإشعار مطلوبان."; return RedirectToAction(nameof(Details), new { id }); }
        _context.OrganizationNotifications.Add(new OrganizationNotification { OrganizationAccountId = id, Title = title.Trim(), Message = message.Trim(), IsRead = false, CreatedAt = DateTime.Now });
        await _context.SaveChangesAsync();
        TempData["Success"] = "تم إرسال الإشعار داخل بوابة الجهة وإلى بريدها الإلكتروني.";
        return RedirectToAction(nameof(Details), new { id });
    }
    private int? GetCurrentAdminId()
    {
        var value = User.FindFirst("KafoAdminUserId")?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }

}
