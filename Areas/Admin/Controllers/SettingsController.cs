using Kafo.Web.Data;
using Kafo.Web.Security;
using Kafo.Web.Services.Interfaces;
using Kafo.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class SettingsController : Controller
{
    private const string DonorType = "Donor";
    private const string OrganizationType = "Organization";

    private readonly ApplicationDbContext _context;
    private readonly IPasswordSetupService _passwordSetup;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ApplicationDbContext context,
        IPasswordSetupService passwordSetup,
        ILogger<SettingsController> logger)
    {
        _context = context;
        _passwordSetup = passwordSetup;
        _logger = logger;
    }

    [HttpGet("/Admin/Settings")]
    public async Task<IActionResult> Index(
        string? q,
        string? accountType,
        string? status,
        CancellationToken cancellationToken)
    {
        q = q?.Trim();
        accountType = NormalizeAccountType(accountType);
        status = status?.Trim().ToLowerInvariant();

        var accounts = new List<AdminPortalAccountRowViewModel>();

        if (string.IsNullOrWhiteSpace(accountType) || accountType == DonorType)
        {
            var donorsQuery = _context.DonorAccounts.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                donorsQuery = donorsQuery.Where(x =>
                    x.FullName.Contains(q) ||
                    (x.OrganizationName != null && x.OrganizationName.Contains(q)) ||
                    (x.Email != null && x.Email.Contains(q)) ||
                    (x.Phone != null && x.Phone.Contains(q)));
            }

            donorsQuery = ApplyStatusFilter(donorsQuery, status);

            var donors = await donorsQuery
                .OrderByDescending(x => x.UpdatedAt)
                .ToListAsync(cancellationToken);

            accounts.AddRange(donors.Select(x => new AdminPortalAccountRowViewModel
            {
                Id = x.Id,
                AccountType = DonorType,
                AccountTypeLabel = "داعم",
                DisplayName = x.FullName,
                SecondaryName = string.IsNullOrWhiteSpace(x.OrganizationName) ? x.DonorType : x.OrganizationName,
                Email = x.Email ?? string.Empty,
                Phone = x.Phone,
                IsActive = x.IsActive,
                EmailNeedsAttention = !PortalEmailPolicy.IsDeliverable(x.Email),
                LastLoginAt = x.LastLoginAt,
                CreatedAt = x.CreatedAt,
                DetailsUrl = $"/Admin/Donors/Details/{x.Id}"
            }));
        }

        if (string.IsNullOrWhiteSpace(accountType) || accountType == OrganizationType)
        {
            var organizationsQuery = _context.OrganizationAccounts.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                organizationsQuery = organizationsQuery.Where(x =>
                    x.Name.Contains(q) ||
                    (x.ContactName != null && x.ContactName.Contains(q)) ||
                    (x.Activity != null && x.Activity.Contains(q)) ||
                    (x.Email != null && x.Email.Contains(q)) ||
                    (x.Phone != null && x.Phone.Contains(q)));
            }

            organizationsQuery = ApplyStatusFilter(organizationsQuery, status);

            var organizations = await organizationsQuery
                .OrderByDescending(x => x.UpdatedAt)
                .ToListAsync(cancellationToken);

            accounts.AddRange(organizations.Select(x => new AdminPortalAccountRowViewModel
            {
                Id = x.Id,
                AccountType = OrganizationType,
                AccountTypeLabel = "جهة / شركة",
                DisplayName = x.Name,
                SecondaryName = string.IsNullOrWhiteSpace(x.ContactName) ? x.Activity : x.ContactName,
                Email = x.Email ?? string.Empty,
                Phone = x.Phone,
                IsActive = x.IsActive,
                EmailNeedsAttention = !PortalEmailPolicy.IsDeliverable(x.Email),
                LastLoginAt = x.LastLoginAt,
                CreatedAt = x.CreatedAt,
                DetailsUrl = $"/Admin/Organizations/Details/{x.Id}"
            }));
        }

        var donorSummary = await _context.DonorAccounts
            .AsNoTracking()
            .Select(x => new { x.IsActive, x.Email })
            .ToListAsync(cancellationToken);

        var organizationSummary = await _context.OrganizationAccounts
            .AsNoTracking()
            .Select(x => new { x.IsActive, x.Email })
            .ToListAsync(cancellationToken);

        var model = new AdminAccountSettingsIndexViewModel
        {
            Search = q ?? string.Empty,
            AccountTypeFilter = accountType ?? string.Empty,
            StatusFilter = status ?? string.Empty,
            Accounts = accounts
                .OrderByDescending(x => x.EmailNeedsAttention)
                .ThenBy(x => x.AccountTypeLabel)
                .ThenBy(x => x.DisplayName)
                .ToList(),
            TotalDonors = donorSummary.Count,
            TotalOrganizations = organizationSummary.Count,
            TotalAccounts = donorSummary.Count + organizationSummary.Count,
            ActiveAccounts = donorSummary.Count(x => x.IsActive) + organizationSummary.Count(x => x.IsActive),
            AccountsNeedingEmailUpdate = donorSummary.Count(x => !PortalEmailPolicy.IsDeliverable(x.Email)) +
                                          organizationSummary.Count(x => !PortalEmailPolicy.IsDeliverable(x.Email))
        };

        return View(model);
    }

    [HttpPost("/Admin/Settings/PortalAccounts/Update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePortalAccount(
        AdminPortalAccountUpdateViewModel model,
        CancellationToken cancellationToken)
    {
        model.AccountType = NormalizeAccountType(model.AccountType) ?? string.Empty;
        model.DisplayName = model.DisplayName?.Trim() ?? string.Empty;
        model.Email = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        model.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();

        if (!IsSupportedType(model.AccountType))
            ModelState.AddModelError(nameof(model.AccountType), "نوع الحساب غير صحيح.");

        if (!PortalEmailPolicy.IsDeliverable(model.Email))
            ModelState.AddModelError(nameof(model.Email), "أدخل بريدًا إلكترونيًا حقيقيًا يمكنه استقبال رمز OTP. لا يمكن استخدام نطاق .local أو نطاقات الاختبار.");

        if (ModelState.IsValid && await EmailExistsAsync(
                model.Email,
                model.AccountType,
                model.Id,
                cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Email), "البريد الإلكتروني مستخدم في حساب داعم أو جهة أخرى.");
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage)
                .Where(x => !string.IsNullOrWhiteSpace(x)));
            return Redirect("/Admin/Settings");
        }

        if (model.AccountType == DonorType)
        {
            var donor = await _context.DonorAccounts
                .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

            if (donor == null)
                return NotFound();

            donor.FullName = model.DisplayName;
            donor.Email = model.Email;
            donor.Phone = model.Phone;
            donor.IsActive = model.IsActive;
            donor.UpdatedAt = DateTime.Now;
        }
        else
        {
            var organization = await _context.OrganizationAccounts
                .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

            if (organization == null)
                return NotFound();

            organization.Name = model.DisplayName;
            organization.Email = model.Email;
            organization.Phone = model.Phone;
            organization.IsActive = model.IsActive;
            organization.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "تم تحديث بيانات حساب الدخول بنجاح. يمكنك الآن إرسال رابط آمن لإعداد كلمة المرور إلى البريد الجديد.";
        return Redirect("/Admin/Settings");
    }

    [HttpPost("/Admin/Settings/PortalAccounts/SendPasswordSetupLink")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPasswordSetupLink(
        AdminPortalPasswordResetViewModel model,
        CancellationToken cancellationToken)
    {
        model.AccountType = NormalizeAccountType(model.AccountType) ?? string.Empty;

        if (!IsSupportedType(model.AccountType) || model.Id <= 0)
        {
            TempData["Error"] = "بيانات الحساب غير صحيحة.";
            return Redirect("/Admin/Settings");
        }

        string displayName;
        string email;
        string accountTypeLabel;
        string oldStamp;
        bool oldMustChange;

        if (model.AccountType == DonorType)
        {
            var donor = await _context.DonorAccounts
                .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
            if (donor == null) return NotFound();

            displayName = donor.FullName;
            email = donor.Email ?? string.Empty;
            accountTypeLabel = "بوابة الداعمين";
            oldStamp = donor.SecurityStamp;
            oldMustChange = donor.MustChangePassword;
            donor.SecurityStamp = LoginSecurity.NewSecurityStamp();
            donor.MustChangePassword = true;
            donor.UpdatedAt = DateTime.Now;
        }
        else
        {
            var organization = await _context.OrganizationAccounts
                .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
            if (organization == null) return NotFound();

            displayName = organization.Name;
            email = organization.Email ?? string.Empty;
            accountTypeLabel = "بوابة الجهات والشركات";
            oldStamp = organization.SecurityStamp;
            oldMustChange = organization.MustChangePassword;
            organization.SecurityStamp = LoginSecurity.NewSecurityStamp();
            organization.MustChangePassword = true;
            organization.UpdatedAt = DateTime.Now;
        }

        if (!PortalEmailPolicy.IsDeliverable(email))
        {
            TempData["Error"] = "لا يمكن إرسال الرابط لأن البريد الحالي وهمي أو غير صالح. عدّل البريد أولًا ثم أعد المحاولة.";
            return Redirect("/Admin/Settings");
        }

        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _passwordSetup.IssueAsync(
                model.AccountType,
                model.Id,
                email,
                displayName,
                accountTypeLabel,
                GetCurrentAdminId(),
                HttpContext,
                cancellationToken);

            TempData["Success"] = $"تم إرسال رابط آمن لإعداد كلمة المرور إلى {email}.";
        }
        catch (Exception ex)
        {
            if (model.AccountType == DonorType)
            {
                var donor = await _context.DonorAccounts.FirstAsync(x => x.Id == model.Id, CancellationToken.None);
                donor.SecurityStamp = oldStamp;
                donor.MustChangePassword = oldMustChange;
                donor.UpdatedAt = DateTime.Now;
            }
            else
            {
                var organization = await _context.OrganizationAccounts.FirstAsync(x => x.Id == model.Id, CancellationToken.None);
                organization.SecurityStamp = oldStamp;
                organization.MustChangePassword = oldMustChange;
                organization.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync(CancellationToken.None);
            _logger.LogError(
                ex,
                "Unable to send password setup link for {AccountType} account {AccountId}",
                model.AccountType,
                model.Id);
            TempData["Error"] = "تعذر إرسال رابط إعداد كلمة المرور، وتمت استعادة حالة الحساب السابقة.";
        }

        return Redirect("/Admin/Settings");
    }

    private async Task<bool> EmailExistsAsync(
        string email,
        string accountType,
        int accountId,
        CancellationToken cancellationToken)
    {
        var donorExists = await _context.DonorAccounts.AnyAsync(x =>
                x.Email != null &&
                x.Email.ToLower() == email &&
                (accountType != DonorType || x.Id != accountId),
            cancellationToken);

        if (donorExists)
            return true;

        return await _context.OrganizationAccounts.AnyAsync(x =>
                x.Email != null &&
                x.Email.ToLower() == email &&
                (accountType != OrganizationType || x.Id != accountId),
            cancellationToken);
    }

    private static IQueryable<Kafo.Web.Models.Donors.DonorAccount> ApplyStatusFilter(
        IQueryable<Kafo.Web.Models.Donors.DonorAccount> query,
        string? status)
        => status switch
        {
            "active" => query.Where(x => x.IsActive),
            "disabled" => query.Where(x => !x.IsActive),
            _ => query
        };

    private static IQueryable<Kafo.Web.Models.Organizations.OrganizationAccount> ApplyStatusFilter(
        IQueryable<Kafo.Web.Models.Organizations.OrganizationAccount> query,
        string? status)
        => status switch
        {
            "active" => query.Where(x => x.IsActive),
            "disabled" => query.Where(x => !x.IsActive),
            _ => query
        };

    private static string? NormalizeAccountType(string? accountType)
    {
        if (string.Equals(accountType, DonorType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(accountType, "donor", StringComparison.OrdinalIgnoreCase))
            return DonorType;

        if (string.Equals(accountType, OrganizationType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(accountType, "organization", StringComparison.OrdinalIgnoreCase))
            return OrganizationType;

        return string.IsNullOrWhiteSpace(accountType) ? null : accountType.Trim();
    }

    private static bool IsSupportedType(string? accountType)
        => accountType is DonorType or OrganizationType;

    private int? GetCurrentAdminId()
    {
        var value = User.FindFirst("KafoAdminUserId")?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }

}
