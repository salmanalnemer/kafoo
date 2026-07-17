using Kafo.Web.Data;
using Kafo.Web.Models.Donors;
using Kafo.Web.Security;
using Kafo.Web.Services;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AdminDonorsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly IPasswordSetupService _passwordSetup;
    private readonly ILogger<AdminDonorsController> _logger;

    public AdminDonorsController(
        ApplicationDbContext context,
        IFileUploadService files,
        IPasswordSetupService passwordSetup,
        ILogger<AdminDonorsController> logger)
    {
        _context = context;
        _files = files;
        _passwordSetup = passwordSetup;
        _logger = logger;
    }

    [HttpGet("/Admin/Donors")]
    public async Task<IActionResult> Index(string? q, string? status)
    {
        q = q?.Trim();
        status = status?.Trim();

        var donorsQuery = _context.DonorAccounts
            .AsNoTracking()
            .Include(x => x.Contributions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            donorsQuery = donorsQuery.Where(x =>
                x.FullName.Contains(q) ||
                (x.Email != null && x.Email.Contains(q)) ||
                (x.Phone != null && x.Phone.Contains(q)) ||
                (x.OrganizationName != null && x.OrganizationName.Contains(q)));
        }

        if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
            donorsQuery = donorsQuery.Where(x => x.IsActive);

        if (string.Equals(status, "disabled", StringComparison.OrdinalIgnoreCase))
            donorsQuery = donorsQuery.Where(x => !x.IsActive);

        var donors = await donorsQuery
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var contributionsQuery = _context.DonorContributions.AsNoTracking();

        var pendingContributions = await _context.DonorContributions
            .AsNoTracking()
            .Include(x => x.DonorAccount)
            .Include(x => x.ProgramProject)
            .Where(x => x.Status == "بانتظار الاعتماد")
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .ToListAsync();

        var model = new AdminDonorsIndexViewModel
        {
            Search = q ?? string.Empty,
            StatusFilter = status ?? string.Empty,
            Donors = donors,
            PendingContributions = pendingContributions,
            TotalDonors = await _context.DonorAccounts.CountAsync(),
            ActiveDonors = await _context.DonorAccounts.CountAsync(x => x.IsActive),
            PendingSupportRequests = await contributionsQuery.CountAsync(x => x.Status == "بانتظار الاعتماد"),
            OpenContributions = await contributionsQuery.CountAsync(x => x.Status != "مكتمل" && x.Status != "مغلق"),
            TotalSupportAmount = await contributionsQuery.SumAsync(x => (decimal?)x.TotalAmount) ?? 0,
            RemainingAmount = await contributionsQuery.SumAsync(x => (decimal?)x.RemainingAmount) ?? 0,
            UnreadNotifications = await _context.DonorNotifications.CountAsync(x => !x.IsRead)
        };

        return View("~/Areas/Admin/Views/Donors/Index.cshtml", model);
    }

    [HttpGet("/Admin/Donors/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var donor = await _context.DonorAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (donor == null)
            return NotFound();

        var contributions = await _context.DonorContributions
            .AsNoTracking()
            .Include(x => x.ProgramProject)
            .Include(x => x.Certificate)
            .Include(x => x.Updates.OrderByDescending(u => u.CreatedAt))
            .Include(x => x.Reports.OrderByDescending(r => r.ReportDate))
            .Include(x => x.SurplusDecisions.OrderByDescending(s => s.ApprovedAt))
            .Where(x => x.DonorAccountId == id)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var contributionIds = contributions.Select(x => x.Id).ToList();

        var model = new AdminDonorDetailsViewModel
        {
            Donor = donor,
            Contributions = contributions,
            Notifications = await _context.DonorNotifications
                .AsNoTracking()
                .Where(x => x.DonorAccountId == id)
                .OrderByDescending(x => x.CreatedAt)
                .Take(20)
                .ToListAsync(),
            Reports = await _context.DonorReports
                .AsNoTracking()
                .Include(x => x.DonorContribution)
                .Where(x => contributionIds.Contains(x.DonorContributionId))
                .OrderByDescending(x => x.ReportDate)
                .Take(20)
                .ToListAsync(),
            SurplusDecisions = await _context.DonorSurplusDecisions
                .AsNoTracking()
                .Include(x => x.DonorContribution)
                .Where(x => contributionIds.Contains(x.DonorContributionId))
                .OrderByDescending(x => x.ApprovedAt)
                .Take(20)
                .ToListAsync(),
            TotalAmount = contributions.Sum(x => x.TotalAmount),
            SpentAmount = contributions.Sum(x => x.SpentAmount),
            RemainingAmount = contributions.Sum(x => x.RemainingAmount),
            BeneficiariesCount = contributions.Sum(x => x.BeneficiariesCount),
            AverageProgress = contributions.Count == 0 ? 0 : Math.Round(contributions.Average(x => x.ProgressPercent), 1),
            PendingRequests = contributions.Count(x => x.Status == "بانتظار الاعتماد")
        };

        return View("~/Areas/Admin/Views/Donors/Details.cshtml", model);
    }

    [HttpGet("/Admin/Donors/Create")]
    public IActionResult Create()
    {
        return View("~/Areas/Admin/Views/Donors/Create.cshtml", new AdminDonorAccountFormViewModel
        {
            IsActive = true,
            DonorType = "فرد"
        });
    }

    [HttpPost("/Admin/Donors/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminDonorAccountFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Email))
            ModelState.AddModelError(nameof(model.Email), "البريد الإلكتروني مطلوب لتسجيل الدخول وإرسال رمز التحقق.");
        else if (!PortalEmailPolicy.IsDeliverable(model.Email))
            ModelState.AddModelError(nameof(model.Email), "أدخل بريدًا إلكترونيًا حقيقيًا يمكنه استقبال رمز OTP.");

        ModelState.Remove(nameof(model.Password));

        await ValidateUniqueDonorAsync(model);

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            foreach (var error in PasswordPolicy.Validate(model.Password))
                ModelState.AddModelError(nameof(model.Password), error);
        }

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/Donors/Create.cshtml", model);

        var initialSecret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
        var hashed = AdminPasswordHasher.HashPassword(initialSecret);

        var donor = new DonorAccount
        {
            FullName = model.FullName.Trim(),
            DonorType = model.DonorType.Trim(),
            OrganizationName = string.IsNullOrWhiteSpace(model.OrganizationName) ? null : model.OrganizationName.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
            PasswordHash = hashed.Hash,
            PasswordSalt = hashed.Salt,
            SecurityStamp = LoginSecurity.NewSecurityStamp(),
            MustChangePassword = true,
            IsActive = model.IsActive,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.DonorAccounts.Add(donor);
        await _context.SaveChangesAsync();

        _context.DonorNotifications.Add(new DonorNotification
        {
            DonorAccountId = donor.Id,
            Title = "تم إنشاء حسابك في بوابة الداعمين",
            Message = "يمكنك الآن الدخول إلى بوابة كفو لمتابعة مساهماتك وتقارير الأثر والإفصاح.",
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إنشاء حساب الداعم بنجاح.";

        try
        {
            await _passwordSetup.IssueAsync(
                "Donor",
                donor.Id,
                donor.Email!,
                donor.FullName,
                "بوابة الداعمين",
                GetCurrentAdminId(),
                HttpContext);
            TempData["Success"] = "تم إنشاء حساب الداعم وإرسال رابط آمن لإعداد كلمة المرور.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to send new donor account credentials for donor {DonorId}", donor.Id);
            TempData["Error"] = "تم إنشاء الحساب، لكن تعذر إرسال رابط إعداد كلمة المرور. يمكنك إعادة إرساله من الإعدادات العامة.";
        }

        return Redirect($"/Admin/Donors/Details/{donor.Id}");
    }

    [HttpGet("/Admin/Donors/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var donor = await _context.DonorAccounts.FindAsync(id);
        if (donor == null)
            return NotFound();

        var model = new AdminDonorAccountFormViewModel
        {
            Id = donor.Id,
            FullName = donor.FullName,
            DonorType = donor.DonorType,
            OrganizationName = donor.OrganizationName,
            Email = donor.Email ?? string.Empty,
            Phone = donor.Phone,
            IsActive = donor.IsActive
        };

        return View("~/Areas/Admin/Views/Donors/Edit.cshtml", model);
    }

    [HttpPost("/Admin/Donors/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminDonorAccountFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (string.IsNullOrWhiteSpace(model.Email))
            ModelState.AddModelError(nameof(model.Email), "البريد الإلكتروني مطلوب لتسجيل الدخول وإرسال رمز التحقق.");
        else if (!PortalEmailPolicy.IsDeliverable(model.Email))
            ModelState.AddModelError(nameof(model.Email), "أدخل بريدًا إلكترونيًا حقيقيًا يمكنه استقبال رمز OTP.");

        await ValidateUniqueDonorAsync(model);

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/Donors/Edit.cshtml", model);

        var donor = await _context.DonorAccounts.FindAsync(id);
        if (donor == null)
            return NotFound();

        donor.FullName = model.FullName.Trim();
        donor.DonorType = model.DonorType.Trim();
        donor.OrganizationName = string.IsNullOrWhiteSpace(model.OrganizationName) ? null : model.OrganizationName.Trim();
        donor.Email = model.Email.Trim().ToLowerInvariant();
        donor.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        donor.IsActive = model.IsActive;
        donor.UpdatedAt = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var hashed = AdminPasswordHasher.HashPassword(model.Password);
            donor.PasswordHash = hashed.Hash;
            donor.PasswordSalt = hashed.Salt;
            donor.SecurityStamp = LoginSecurity.NewSecurityStamp();
            donor.MustChangePassword = false;
            donor.AccessFailedCount = 0;
            donor.LockoutEndUtc = null;
            donor.PasswordChangedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تحديث بيانات الداعم بنجاح.";
        return Redirect($"/Admin/Donors/Details/{donor.Id}");
    }

    [HttpPost("/Admin/Donors/Toggle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var donor = await _context.DonorAccounts.FindAsync(id);
        if (donor == null)
            return NotFound();

        donor.IsActive = !donor.IsActive;
        donor.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["Success"] = donor.IsActive ? "تم تفعيل حساب الداعم." : "تم تعطيل حساب الداعم.";
        return Redirect($"/Admin/Donors/Details/{donor.Id}");
    }


    [HttpGet("/Admin/Donors/Contribution/Create/{donorId:int}")]
    public IActionResult CreateContribution(int donorId)
    {
        TempData["Error"] = "إضافة المساهمات تتم من بوابة الداعم. ادخل على طلب الداعم ثم اعتمده أو حدث التنفيذ.";
        return Redirect($"/Admin/Donors/Details/{donorId}");
    }

    [HttpPost("/Admin/Donors/Contribution/Create/{donorId:int}")]
    [ValidateAntiForgeryToken]
    public IActionResult CreateContribution(int donorId, AdminDonorContributionFormViewModel model)
    {
        TempData["Error"] = "إضافة المساهمات تتم من بوابة الداعم، ولا يتم إدخالها يدوياً من الإدارة.";
        return Redirect($"/Admin/Donors/Details/{donorId}");
    }

    [HttpPost("/Admin/Donors/Contribution/Approve/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveContribution(int id)
    {
        var contribution = await _context.DonorContributions
            .Include(x => x.ProgramProject)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (contribution == null)
            return NotFound();

        if (!string.Equals(contribution.Status, "بانتظار الاعتماد", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "هذا الطلب تم التعامل معه مسبقاً.";
            return Redirect($"/Admin/Donors/Details/{contribution.DonorAccountId}");
        }

        contribution.Status = "قيد التنفيذ";
        contribution.ProgressPercent = 0;
        contribution.StartedAt ??= DateTime.Today;
        RecalculateContribution(contribution);
        contribution.UpdatedAt = DateTime.Now;

        _context.DonorContributionUpdates.Add(new DonorContributionUpdate
        {
            DonorContributionId = contribution.Id,
            Title = "تم اعتماد الدعم وتوجيهه للبرنامج",
            Details = $"تم اعتماد طلب الدعم بقيمة {contribution.TotalAmount:N2} ر.س للبرنامج: {(contribution.ProgramProject?.Title ?? contribution.Title)}.",
            ProgressPercent = contribution.ProgressPercent,
            CreatedAt = DateTime.Now
        });

        _context.DonorNotifications.Add(new DonorNotification
        {
            DonorAccountId = contribution.DonorAccountId,
            DonorContributionId = contribution.Id,
            Title = "تم اعتماد الدعم",
            Message = $"تم اعتماد دعمك وتوجيهه إلى {(contribution.ProgramProject?.Title ?? contribution.Title)} بقيمة {contribution.TotalAmount:N2} ر.س.",
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = "تم اعتماد طلب الدعم وإشعار الداعم.";
        return Redirect($"/Admin/Donors/Details/{contribution.DonorAccountId}");
    }

    [HttpPost("/Admin/Donors/Contribution/Reject/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectContribution(int id, string? reason)
    {
        var contribution = await _context.DonorContributions.FindAsync(id);
        if (contribution == null)
            return NotFound();

        if (!string.Equals(contribution.Status, "بانتظار الاعتماد", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "لا يمكن رفض مساهمة ليست بانتظار الاعتماد.";
            return Redirect($"/Admin/Donors/Details/{contribution.DonorAccountId}");
        }

        var rejectReason = string.IsNullOrWhiteSpace(reason) ? "لم يتم توضيح السبب" : reason.Trim();

        contribution.Status = "مغلق";
        contribution.ProgressPercent = 0;
        contribution.SpentAmount = 0;
        contribution.RemainingAmount = contribution.TotalAmount;
        contribution.HasSurplus = false;
        contribution.IsSurplusLocked = false;
        contribution.CompletedAt = DateTime.Now;
        contribution.UpdatedAt = DateTime.Now;

        _context.DonorContributionUpdates.Add(new DonorContributionUpdate
        {
            DonorContributionId = contribution.Id,
            Title = "تم إغلاق طلب الدعم",
            Details = $"تم إغلاق الطلب من الإدارة. السبب: {rejectReason}",
            ProgressPercent = 0,
            CreatedAt = DateTime.Now
        });

        _context.DonorNotifications.Add(new DonorNotification
        {
            DonorAccountId = contribution.DonorAccountId,
            DonorContributionId = contribution.Id,
            Title = "تم تحديث طلب الدعم",
            Message = $"تم إغلاق طلب الدعم: {contribution.Title}. السبب: {rejectReason}",
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = "تم إغلاق طلب الدعم وإشعار الداعم.";
        return Redirect($"/Admin/Donors/Details/{contribution.DonorAccountId}");
    }

    [HttpGet("/Admin/Donors/Contribution/Edit/{id:int}")]
    public async Task<IActionResult> EditContribution(int id)
    {
        var contribution = await _context.DonorContributions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (contribution == null)
            return NotFound();

        await FillProgramsAsync();
        return View("~/Areas/Admin/Views/Donors/EditContribution.cshtml", ToContributionForm(contribution));
    }

    [HttpPost("/Admin/Donors/Contribution/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditContribution(int id, AdminDonorContributionFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        var contribution = await _context.DonorContributions.FindAsync(id);
        if (contribution == null)
            return NotFound();

        var spent = model.SpentAmount < 0 ? 0 : model.SpentAmount;
        if (spent > contribution.TotalAmount)
            ModelState.AddModelError(nameof(model.SpentAmount), "المبلغ المصروف لا يمكن أن يتجاوز قيمة الدعم المرسلة من الداعم.");

        if (!ModelState.IsValid)
        {
            await FillProgramsAsync();
            model.TotalAmount = contribution.TotalAmount;
            model.RemainingAmount = Math.Max(0, contribution.TotalAmount - spent);
            model.Title = contribution.Title;
            return View("~/Areas/Admin/Views/Donors/EditContribution.cshtml", model);
        }

        var oldStatus = contribution.Status;
        var oldProgress = contribution.ProgressPercent;
        var oldSpent = contribution.SpentAmount;
        var oldRemaining = contribution.RemainingAmount;

        // بيانات الطلب الأصلية مثل القيمة والعنوان تأتي من الداعم ولا يعاد إدخالها من الإدارة.
        contribution.ProgramProjectId = model.ProgramProjectId;
        contribution.Status = model.Status.Trim();
        contribution.ProgressPercent = Math.Clamp(model.ProgressPercent, 0, 100);
        contribution.SpentAmount = spent;
        contribution.BeneficiariesCount = Math.Max(0, model.BeneficiariesCount);
        contribution.ImpactSummary = string.IsNullOrWhiteSpace(model.ImpactSummary) ? contribution.ImpactSummary : model.ImpactSummary.Trim();
        contribution.StartedAt = model.StartedAt;
        contribution.CompletedAt = model.CompletedAt;

        RecalculateContribution(contribution);

        if (IsCompletedStatus(contribution.Status) && contribution.CompletedAt == null)
            contribution.CompletedAt = DateTime.Now;

        contribution.UpdatedAt = DateTime.Now;

        if (oldStatus != contribution.Status || oldProgress != contribution.ProgressPercent || oldSpent != contribution.SpentAmount || oldRemaining != contribution.RemainingAmount)
        {
            _context.DonorContributionUpdates.Add(new DonorContributionUpdate
            {
                DonorContributionId = contribution.Id,
                Title = "تحديث تنفيذ الدعم",
                Details = $"الحالة: {contribution.Status}، نسبة الإنجاز: {contribution.ProgressPercent}%، المصروف: {contribution.SpentAmount:N2} ر.س، المتبقي محسوب آلياً: {contribution.RemainingAmount:N2} ر.س.",
                ProgressPercent = contribution.ProgressPercent,
                CreatedAt = DateTime.Now
            });

            _context.DonorNotifications.Add(new DonorNotification
            {
                DonorAccountId = contribution.DonorAccountId,
                DonorContributionId = contribution.Id,
                Title = "تم تحديث تنفيذ دعمك",
                Message = $"تم تحديث مساهمتك: {contribution.Title}. الحالة: {contribution.Status}، الإنجاز {contribution.ProgressPercent}%، والمتبقي {contribution.RemainingAmount:N2} ر.س.",
                CreatedAt = DateTime.Now
            });
        }

        if (contribution.HasSurplus && IsCompletedStatus(contribution.Status))
        {
            _context.DonorNotifications.Add(new DonorNotification
            {
                DonorAccountId = contribution.DonorAccountId,
                DonorContributionId = contribution.Id,
                Title = "يوجد فائض دعم بانتظار موافقتك",
                Message = $"يوجد فائض محسوب آلياً بقيمة {contribution.RemainingAmount:N2} ر.س في مساهمتك: {contribution.Title}. لا يتم التصرف فيه إلا بعد موافقتك الإلكترونية.",
                CreatedAt = DateTime.Now
            });
        }

        var certificateIssued = false;
        if (IsCompletedStatus(contribution.Status) && !string.Equals(oldStatus, "بانتظار الاعتماد", StringComparison.OrdinalIgnoreCase))
        {
            certificateIssued = await EnsureContributionCertificateAsync(contribution);
            if (certificateIssued)
            {
                _context.DonorContributionUpdates.Add(new DonorContributionUpdate
                {
                    DonorContributionId = contribution.Id,
                    Title = "تم إصدار شهادة عطاء إلكترونية",
                    Details = "تم إصدار شهادة عطاء إلكترونية للداعم بعد إغلاق رحلة الدعم من الإدارة.",
                    ProgressPercent = contribution.ProgressPercent,
                    CreatedAt = DateTime.Now
                });

                _context.DonorNotifications.Add(new DonorNotification
                {
                    DonorAccountId = contribution.DonorAccountId,
                    DonorContributionId = contribution.Id,
                    Title = "تم إصدار شهادة عطاء",
                    Message = $"تم إصدار شهادة عطاء إلكترونية لمساهمتك: {contribution.Title}. يمكنك عرضها وطباعتها من تفاصيل المساهمة.",
                    CreatedAt = DateTime.Now
                });
            }
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = certificateIssued
            ? "تم تحديث التنفيذ وإصدار شهادة عطاء إلكترونية للداعم."
            : "تم تحديث التنفيذ، وتم احتساب المتبقي والفائض آلياً.";
        return Redirect($"/Admin/Donors/Details/{contribution.DonorAccountId}");
    }

    [HttpPost("/Admin/Donors/Contribution/Close/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseContribution(int id)
    {
        var contribution = await _context.DonorContributions
            .Include(x => x.Certificate)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (contribution == null)
            return NotFound();

        if (string.Equals(contribution.Status, "بانتظار الاعتماد", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "لا يمكن إصدار شهادة قبل اعتماد طلب الدعم وبدء التنفيذ.";
            return Redirect($"/Admin/Donors/Details/{contribution.DonorAccountId}");
        }

        var wasAlreadyCompleted = IsCompletedStatus(contribution.Status);

        contribution.Status = "مكتمل";
        contribution.ProgressPercent = 100;
        contribution.CompletedAt ??= DateTime.Now;
        RecalculateContribution(contribution);
        contribution.UpdatedAt = DateTime.Now;

        var certificateIssued = await EnsureContributionCertificateAsync(contribution);

        if (!wasAlreadyCompleted)
        {
            _context.DonorContributionUpdates.Add(new DonorContributionUpdate
            {
                DonorContributionId = contribution.Id,
                Title = "تم إغلاق رحلة الدعم",
                Details = $"تم إغلاق رحلة الدعم من الإدارة بنسبة إنجاز {contribution.ProgressPercent}%، وتم إصدار شهادة عطاء إلكترونية.",
                ProgressPercent = contribution.ProgressPercent,
                CreatedAt = DateTime.Now
            });

            _context.DonorNotifications.Add(new DonorNotification
            {
                DonorAccountId = contribution.DonorAccountId,
                DonorContributionId = contribution.Id,
                Title = "تم إغلاق رحلة الدعم",
                Message = $"تم إغلاق رحلة الدعم الخاصة بمساهمتك: {contribution.Title}، وتم إصدار شهادة عطاء إلكترونية باسمك.",
                CreatedAt = DateTime.Now
            });
        }
        else if (certificateIssued)
        {
            _context.DonorContributionUpdates.Add(new DonorContributionUpdate
            {
                DonorContributionId = contribution.Id,
                Title = "تم إصدار شهادة عطاء إلكترونية",
                Details = "تم إصدار شهادة عطاء إلكترونية للداعم بعد إغلاق رحلة الدعم من الإدارة.",
                ProgressPercent = contribution.ProgressPercent,
                CreatedAt = DateTime.Now
            });

            _context.DonorNotifications.Add(new DonorNotification
            {
                DonorAccountId = contribution.DonorAccountId,
                DonorContributionId = contribution.Id,
                Title = "تم إصدار شهادة عطاء",
                Message = $"تم إصدار شهادة عطاء إلكترونية لمساهمتك: {contribution.Title}. يمكنك عرضها وطباعتها من تفاصيل المساهمة.",
                CreatedAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = certificateIssued
            ? "تم إغلاق رحلة الدعم وإصدار شهادة عطاء إلكترونية."
            : "رحلة الدعم مغلقة مسبقاً والشهادة موجودة.";

        return Redirect($"/Admin/Donors/Details/{contribution.DonorAccountId}");
    }

    [HttpGet("/Admin/Donors/Contribution/Certificate/{id:int}")]
    public async Task<IActionResult> ContributionCertificate(int id)
    {
        var certificate = await GetCertificateForContributionAsync(id);

        if (certificate == null)
        {
            var contribution = await _context.DonorContributions
                .Include(x => x.Certificate)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (contribution == null)
                return NotFound();

            if (!IsCompletedStatus(contribution.Status) || string.Equals(contribution.Status, "بانتظار الاعتماد", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "لا يمكن عرض الشهادة قبل إغلاق رحلة الدعم.";
                return Redirect($"/Admin/Donors/Details/{contribution.DonorAccountId}");
            }

            await EnsureContributionCertificateAsync(contribution);
            await _context.SaveChangesAsync();
            certificate = await GetCertificateForContributionAsync(id);
        }

        if (certificate == null)
            return NotFound();

        return View("~/Areas/Admin/Views/Donors/Certificate.cshtml", certificate);
    }

    [HttpPost("/Admin/Donors/Contribution/AddUpdate/{contributionId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddContributionUpdate(int contributionId, string title, string? details, int? progressPercent)
    {
        var contribution = await _context.DonorContributions.FindAsync(contributionId);
        if (contribution == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "عنوان التحديث مطلوب.";
            return Redirect($"/Admin/Donors/Details/{contribution.DonorAccountId}");
        }

        if (progressPercent.HasValue)
        {
            contribution.ProgressPercent = Math.Clamp(progressPercent.Value, 0, 100);
            contribution.UpdatedAt = DateTime.Now;
        }

        _context.DonorContributionUpdates.Add(new DonorContributionUpdate
        {
            DonorContributionId = contribution.Id,
            Title = title.Trim(),
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
            ProgressPercent = progressPercent,
            CreatedAt = DateTime.Now
        });

        _context.DonorNotifications.Add(new DonorNotification
        {
            DonorAccountId = contribution.DonorAccountId,
            DonorContributionId = contribution.Id,
            Title = "تحديث جديد على مساهمتك",
            Message = string.IsNullOrWhiteSpace(details) ? title.Trim() : details.Trim(),
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = "تم إضافة التحديث وإشعار الداعم.";
        return Redirect($"/Admin/Donors/Details/{contribution.DonorAccountId}");
    }

    [HttpPost("/Admin/Donors/Contribution/AddReport/{contributionId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReport(int contributionId, string title, string? summary, string? reportType, DateTime? reportDate, IFormFile? file)
    {
        var contribution = await _context.DonorContributions.FindAsync(contributionId);
        if (contribution == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "عنوان التقرير مطلوب.";
            return Redirect($"/Admin/Donors/Details/{contribution.DonorAccountId}");
        }

        string? filePath = null;
        if (file != null && file.Length > 0)
            filePath = await _files.UploadAsync(file, "donor-reports");

        var report = new DonorReport
        {
            DonorContributionId = contribution.Id,
            Title = title.Trim(),
            Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
            FilePath = filePath,
            ReportType = string.IsNullOrWhiteSpace(reportType) ? "تقرير دوري" : reportType.Trim(),
            ReportDate = reportDate ?? DateTime.Now,
            CreatedAt = DateTime.Now
        };

        _context.DonorReports.Add(report);
        _context.DonorNotifications.Add(new DonorNotification
        {
            DonorAccountId = contribution.DonorAccountId,
            DonorContributionId = contribution.Id,
            Title = "تم رفع تقرير جديد",
            Message = $"تم رفع {report.ReportType}: {report.Title}. يمكنك الاطلاع عليه من صفحة التقارير.",
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = "تم رفع التقرير وإشعار الداعم.";
        return Redirect($"/Admin/Donors/Details/{contribution.DonorAccountId}");
    }


    [HttpPost("/Admin/Donors/Contribution/SetSurplus/{contributionId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetSurplus(int contributionId, string? reason)
    {
        var contribution = await _context.DonorContributions.FindAsync(contributionId);
        if (contribution == null)
            return NotFound();

        RecalculateContribution(contribution);
        var surplusAmount = contribution.RemainingAmount;

        if (surplusAmount <= 0)
        {
            contribution.HasSurplus = false;
            contribution.IsSurplusLocked = false;
            contribution.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Error"] = "لا يوجد فائض دعم حالياً، لأن المتبقي المحسوب آلياً يساوي صفر.";
            return Redirect($"/Admin/Donors/Details/{contribution.DonorAccountId}");
        }

        var surplusReason = string.IsNullOrWhiteSpace(reason) ? "وجود مبلغ متبقٍ بعد تنفيذ البرنامج" : reason.Trim();

        contribution.HasSurplus = true;
        contribution.IsSurplusLocked = true;
        contribution.UpdatedAt = DateTime.Now;

        _context.DonorContributionUpdates.Add(new DonorContributionUpdate
        {
            DonorContributionId = contribution.Id,
            Title = "تحديث فائض الدعم",
            Details = $"يوجد فائض دعم محسوب آلياً بقيمة {surplusAmount:N2} ر.س. السبب: {surplusReason}",
            ProgressPercent = contribution.ProgressPercent,
            CreatedAt = DateTime.Now
        });

        _context.DonorNotifications.Add(new DonorNotification
        {
            DonorAccountId = contribution.DonorAccountId,
            DonorContributionId = contribution.Id,
            Title = "يوجد فائض دعم بانتظار موافقتك",
            Message = $"يوجد فائض محسوب آلياً بقيمة {surplusAmount:N2} ر.س في مساهمتك: {contribution.Title}. السبب: {surplusReason}. الرجاء الدخول لصفحة فائض الدعم لاختيار الإجراء المناسب.",
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = "تم احتساب فائض الدعم آلياً وإشعار الداعم.";
        return Redirect($"/Admin/Donors/Details/{contribution.DonorAccountId}");
    }

    [HttpPost("/Admin/Donors/SendNotification/{donorId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendNotification(int donorId, string title, string message, int? contributionId, bool sentBySms)
    {
        var donor = await _context.DonorAccounts.FindAsync(donorId);
        if (donor == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "عنوان ونص الإشعار مطلوبة.";
            return Redirect($"/Admin/Donors/Details/{donorId}");
        }

        _context.DonorNotifications.Add(new DonorNotification
        {
            DonorAccountId = donorId,
            DonorContributionId = contributionId,
            Title = title.Trim(),
            Message = message.Trim(),
            SentBySms = sentBySms,
            SentByEmail = false,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = "تم إرسال الإشعار إلى بوابة الداعم وبريده الإلكتروني.";
        return Redirect($"/Admin/Donors/Details/{donorId}");
    }

    private async Task<DonorContributionCertificate?> GetCertificateForContributionAsync(int contributionId)
    {
        return await _context.DonorContributionCertificates
            .AsNoTracking()
            .Include(x => x.DonorContribution)
                .ThenInclude(x => x!.DonorAccount)
            .Include(x => x.DonorContribution)
                .ThenInclude(x => x!.ProgramProject)
            .FirstOrDefaultAsync(x => x.DonorContributionId == contributionId);
    }

    private async Task<bool> EnsureContributionCertificateAsync(DonorContribution contribution)
    {
        if (!IsCompletedStatus(contribution.Status))
            return false;

        var exists = await _context.DonorContributionCertificates
            .AnyAsync(x => x.DonorContributionId == contribution.Id);

        if (exists)
            return false;

        var donor = await _context.DonorAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == contribution.DonorAccountId);

        if (donor == null)
            return false;

        var programTitle = contribution.ProgramProjectId.HasValue
            ? await _context.ProgramProjects
                .AsNoTracking()
                .Where(x => x.Id == contribution.ProgramProjectId.Value)
                .Select(x => x.Title)
                .FirstOrDefaultAsync()
            : null;

        var executiveManager = await _context.ExecutiveManagerMessages
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync();

        var certificate = new DonorContributionCertificate
        {
            DonorContributionId = contribution.Id,
            CertificateNumber = await DonorCertificateNumberGenerator.GenerateAsync(_context),
            DonorName = donor.FullName,
            DonorOrganizationName = donor.OrganizationName,
            ContributionTitle = contribution.Title,
            ProgramTitle = programTitle,
            ContributionCode = contribution.ContributionCode,
            TotalAmount = contribution.TotalAmount,
            SpentAmount = contribution.SpentAmount,
            RemainingAmount = contribution.RemainingAmount,
            BeneficiariesCount = Math.Max(0, contribution.BeneficiariesCount),
            ImpactSummary = contribution.ImpactSummary,
            ExecutiveManagerName = string.IsNullOrWhiteSpace(executiveManager?.ManagerName) ? "المدير التنفيذي" : executiveManager.ManagerName,
            ExecutiveManagerTitle = string.IsNullOrWhiteSpace(executiveManager?.PositionTitle) ? "المدير التنفيذي" : executiveManager.PositionTitle,
            SignatureImagePath = executiveManager?.SignatureImagePath,
            IssuedAt = DateTime.Now,
            CreatedAt = DateTime.Now
        };

        _context.DonorContributionCertificates.Add(certificate);
        return true;
    }

    private async Task ValidateUniqueDonorAsync(AdminDonorAccountFormViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var email = model.Email.Trim().ToLowerInvariant();
            var donorExists = await _context.DonorAccounts.AnyAsync(x =>
                x.Id != model.Id &&
                x.Email != null &&
                x.Email.ToLower() == email);
            var organizationExists = await _context.OrganizationAccounts.AnyAsync(x =>
                x.Email != null &&
                x.Email.ToLower() == email);

            if (donorExists || organizationExists)
                ModelState.AddModelError(nameof(model.Email), "البريد الإلكتروني مستخدم في حساب داعم أو جهة أخرى.");
        }

        if (!string.IsNullOrWhiteSpace(model.Phone))
        {
            var phone = model.Phone.Trim();
            var exists = await _context.DonorAccounts.AnyAsync(x => x.Id != model.Id && x.Phone == phone);
            if (exists)
                ModelState.AddModelError(nameof(model.Phone), "رقم الجوال مستخدم مسبقاً.");
        }
    }

    private async Task FillProgramsAsync()
    {
        ViewBag.Programs = await _context.ProgramProjects
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Title)
            .ToListAsync();
    }


    private static void RecalculateContribution(DonorContribution contribution)
    {
        if (contribution.TotalAmount < 0)
            contribution.TotalAmount = 0;

        if (contribution.SpentAmount < 0)
            contribution.SpentAmount = 0;

        if (contribution.SpentAmount > contribution.TotalAmount)
            contribution.SpentAmount = contribution.TotalAmount;

        contribution.RemainingAmount = Math.Max(0, contribution.TotalAmount - contribution.SpentAmount);
        contribution.HasSurplus = IsCompletedStatus(contribution.Status) && contribution.RemainingAmount > 0;
        contribution.IsSurplusLocked = contribution.HasSurplus;
    }

    private static bool IsCompletedStatus(string? status)
    {
        return string.Equals(status, "مكتمل", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, "مغلق", StringComparison.OrdinalIgnoreCase);
    }

    private static AdminDonorContributionFormViewModel ToContributionForm(DonorContribution item)
    {
        return new AdminDonorContributionFormViewModel
        {
            Id = item.Id,
            DonorAccountId = item.DonorAccountId,
            ProgramProjectId = item.ProgramProjectId,
            Title = item.Title,
            Status = item.Status,
            ProgressPercent = item.ProgressPercent,
            TotalAmount = item.TotalAmount,
            SpentAmount = item.SpentAmount,
            RemainingAmount = item.RemainingAmount,
            BeneficiariesCount = item.BeneficiariesCount,
            ImpactSummary = item.ImpactSummary,
            HasSurplus = item.HasSurplus,
            IsSurplusLocked = item.IsSurplusLocked,
            StartedAt = item.StartedAt,
            CompletedAt = item.CompletedAt
        };
    }
    private int? GetCurrentAdminId()
    {
        var value = User.FindFirst("KafoAdminUserId")?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }

}
