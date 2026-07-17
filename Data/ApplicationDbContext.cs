using Kafo.Web.Configuration;
using Kafo.Web.Models;
using Kafo.Web.Models.Donors;
using Kafo.Web.Models.Organizations;
using Kafo.Web.Security;
using Kafo.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kafo.Web.Data;

public class ApplicationDbContext : DbContext
{
    private readonly IEmailSender _emailSender;
    private readonly IOptionsMonitor<NotificationEmailOptions> _notificationEmailOptions;
    private readonly ILogger<ApplicationDbContext> _logger;
    private bool _isSavingNotificationDeliveryState;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IEmailSender emailSender,
        IOptionsMonitor<NotificationEmailOptions> notificationEmailOptions,
        ILogger<ApplicationDbContext> logger)
        : base(options)
    {
        _emailSender = emailSender;
        _notificationEmailOptions = notificationEmailOptions;
        _logger = logger;
    }

    public DbSet<Slider> Sliders => Set<Slider>();
    public DbSet<HomeStatistic> HomeStatistics => Set<HomeStatistic>();
    public DbSet<StrategicGoal> StrategicGoals => Set<StrategicGoal>();
    public DbSet<ProgramProject> ProgramProjects => Set<ProgramProject>();
    public DbSet<SuccessPartner> SuccessPartners => Set<SuccessPartner>();
    public DbSet<NewsPost> NewsPosts => Set<NewsPost>();
    public DbSet<VideoLibraryItem> VideoLibraryItems => Set<VideoLibraryItem>();
    public DbSet<AidReport> AidReports => Set<AidReport>();

    public DbSet<GeneralAssemblyMember> GeneralAssemblyMembers => Set<GeneralAssemblyMember>();
    public DbSet<GeneralAssemblyMinute> GeneralAssemblyMinutes => Set<GeneralAssemblyMinute>();
    public DbSet<BoardMember> BoardMembers => Set<BoardMember>();
    public DbSet<CommitteeDocument> CommitteeDocuments => Set<CommitteeDocument>();
    public DbSet<PolicyDocument> PolicyDocuments => Set<PolicyDocument>();
    public DbSet<AnnualReportDocument> AnnualReportDocuments => Set<AnnualReportDocument>();
    public DbSet<QuarterReportDocument> QuarterReportDocuments => Set<QuarterReportDocument>();
    public DbSet<FinancialStatementDocument> FinancialStatementDocuments => Set<FinancialStatementDocument>();
    public DbSet<OperationalPlanDocument> OperationalPlanDocuments => Set<OperationalPlanDocument>();

    public DbSet<VolunteerOpportunity> VolunteerOpportunities => Set<VolunteerOpportunity>();
    public DbSet<Initiative> Initiatives => Set<Initiative>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<SatisfactionResponse> SatisfactionResponses => Set<SatisfactionResponse>();
    public DbSet<FeedbackEntry> FeedbackEntries => Set<FeedbackEntry>();

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AdminPagePermission> AdminPagePermissions => Set<AdminPagePermission>();
    public DbSet<PasswordSetupToken> PasswordSetupTokens => Set<PasswordSetupToken>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
    public DbSet<LoginOtpChallenge> LoginOtpChallenges => Set<LoginOtpChallenge>();

    public DbSet<SiteContentPage> SiteContentPages => Set<SiteContentPage>();
    public DbSet<VisionMissionCard> VisionMissionCards => Set<VisionMissionCard>();
    public DbSet<PresidentMessage> PresidentMessages => Set<PresidentMessage>();
    public DbSet<ExecutiveManagerMessage> ExecutiveManagerMessages => Set<ExecutiveManagerMessage>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<LicenseDocument> LicenseDocuments => Set<LicenseDocument>();
    public DbSet<OrganizationalStructurePage> OrganizationalStructurePages => Set<OrganizationalStructurePage>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<ServiceLink> ServiceLinks => Set<ServiceLink>();

    public DbSet<BeneficiaryDataUpdatePage> BeneficiaryDataUpdatePages => Set<BeneficiaryDataUpdatePage>();
    public DbSet<BeneficiaryDataUpdateRequirement> BeneficiaryDataUpdateRequirements => Set<BeneficiaryDataUpdateRequirement>();

    public DbSet<NewBeneficiaryRegistrationPage> NewBeneficiaryRegistrationPages => Set<NewBeneficiaryRegistrationPage>();
    public DbSet<NewBeneficiaryRegistrationRequirement> NewBeneficiaryRegistrationRequirements => Set<NewBeneficiaryRegistrationRequirement>();

    public DbSet<InKindDonationType> InKindDonationTypes => Set<InKindDonationType>();
    public DbSet<InKindDonationRequest> InKindDonationRequests => Set<InKindDonationRequest>();

    public DbSet<DonorAccount> DonorAccounts => Set<DonorAccount>();
    public DbSet<DonorContribution> DonorContributions => Set<DonorContribution>();
    public DbSet<DonorContributionUpdate> DonorContributionUpdates => Set<DonorContributionUpdate>();
    public DbSet<DonorReport> DonorReports => Set<DonorReport>();
    public DbSet<DonorSurplusDecision> DonorSurplusDecisions => Set<DonorSurplusDecision>();
    public DbSet<DonorContributionCertificate> DonorContributionCertificates => Set<DonorContributionCertificate>();
    public DbSet<DonorNotification> DonorNotifications => Set<DonorNotification>();

    public DbSet<OrganizationAccount> OrganizationAccounts => Set<OrganizationAccount>();
    public DbSet<OpportunityRequest> OpportunityRequests => Set<OpportunityRequest>();
    public DbSet<OpportunityCandidate> OpportunityCandidates => Set<OpportunityCandidate>();
    public DbSet<OrganizationEvaluation> OrganizationEvaluations => Set<OrganizationEvaluation>();
    public DbSet<OrganizationNotification> OrganizationNotifications => Set<OrganizationNotification>();

    public override int SaveChanges()
        => SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_isSavingNotificationDeliveryState)
            return await base.SaveChangesAsync(cancellationToken);

        var donorNotifications = ChangeTracker.Entries<DonorNotification>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => x.Entity)
            .ToList();

        var organizationNotifications = ChangeTracker.Entries<OrganizationNotification>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => x.Entity)
            .ToList();

        // SentByEmail reflects actual SMTP delivery, not a manually selected checkbox.
        foreach (var notification in donorNotifications)
            notification.SentByEmail = false;

        foreach (var notification in organizationNotifications)
            notification.SentByEmail = false;

        var adminEvents = CaptureAdminEmailEvents();
        var result = await base.SaveChangesAsync(cancellationToken);

        var options = _notificationEmailOptions.CurrentValue;
        if (!options.Enabled)
            return result;

        var deliveryStateChanged = false;

        try
        {
            // The business operation is already committed. Email failures must never undo it.
            var deliveryCancellationToken = CancellationToken.None;

            if (options.SendPortalNotifications)
            {
                deliveryStateChanged |= await SendDonorNotificationEmailsAsync(
                    donorNotifications,
                    options,
                    deliveryCancellationToken);

                deliveryStateChanged |= await SendOrganizationNotificationEmailsAsync(
                    organizationNotifications,
                    options,
                    deliveryCancellationToken);
            }

            if (options.SendAdminNotifications)
            {
                await SendAdminEventEmailsAsync(
                    adminEvents,
                    options,
                    deliveryCancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "The business operation was saved, but notification email processing failed.");
        }

        if (deliveryStateChanged)
        {
            try
            {
                _isSavingNotificationDeliveryState = true;
                await base.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Notification emails were sent, but the SentByEmail delivery state could not be persisted.");
            }
            finally
            {
                _isSavingNotificationDeliveryState = false;
            }
        }

        return result;
    }

    private async Task<bool> SendDonorNotificationEmailsAsync(
        IReadOnlyCollection<DonorNotification> notifications,
        NotificationEmailOptions options,
        CancellationToken cancellationToken)
    {
        if (notifications.Count == 0)
            return false;

        var donorIds = notifications
            .Select(x => x.DonorAccountId)
            .Distinct()
            .ToArray();

        var donors = await DonorAccounts
            .AsNoTracking()
            .Where(x => donorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var stateChanged = false;

        foreach (var notification in notifications)
        {
            if (!donors.TryGetValue(notification.DonorAccountId, out var donor) ||
                !IsDeliverableEmail(donor.Email))
            {
                _logger.LogWarning(
                    "Donor notification {NotificationId} was saved without email delivery because donor {DonorId} has no deliverable email address.",
                    notification.Id,
                    notification.DonorAccountId);
                continue;
            }

            var actionPath = notification.DonorContributionId.HasValue
                ? $"/Portal/Donor/Contributions/Details/{notification.DonorContributionId.Value}"
                : "/Portal/Donor/Notifications";

            try
            {
                await _emailSender.SendNotificationAsync(
                    donor.Email!,
                    donor.FullName,
                    "بوابة الداعمين",
                    notification.Title,
                    notification.Message,
                    BuildAbsoluteUrl(options.PublicBaseUrl, actionPath),
                    cancellationToken);

                notification.SentByEmail = true;
                stateChanged = true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to email donor notification {NotificationId} to donor {DonorId}.",
                    notification.Id,
                    notification.DonorAccountId);
            }
        }

        return stateChanged;
    }

    private async Task<bool> SendOrganizationNotificationEmailsAsync(
        IReadOnlyCollection<OrganizationNotification> notifications,
        NotificationEmailOptions options,
        CancellationToken cancellationToken)
    {
        if (notifications.Count == 0)
            return false;

        var organizationIds = notifications
            .Select(x => x.OrganizationAccountId)
            .Distinct()
            .ToArray();

        var organizations = await OrganizationAccounts
            .AsNoTracking()
            .Where(x => organizationIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var stateChanged = false;

        foreach (var notification in notifications)
        {
            if (!organizations.TryGetValue(notification.OrganizationAccountId, out var organization) ||
                !IsDeliverableEmail(organization.Email))
            {
                _logger.LogWarning(
                    "Organization notification {NotificationId} was saved without email delivery because organization {OrganizationId} has no deliverable email address.",
                    notification.Id,
                    notification.OrganizationAccountId);
                continue;
            }

            var actionPath = notification.OpportunityRequestId.HasValue
                ? $"/Portal/Organization/OpportunityRequests/Details/{notification.OpportunityRequestId.Value}"
                : "/Portal/Organization/Notifications";

            try
            {
                await _emailSender.SendNotificationAsync(
                    organization.Email!,
                    organization.Name,
                    "بوابة الجهات والشركات",
                    notification.Title,
                    notification.Message,
                    BuildAbsoluteUrl(options.PublicBaseUrl, actionPath),
                    cancellationToken);

                notification.SentByEmail = true;
                stateChanged = true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to email organization notification {NotificationId} to organization {OrganizationId}.",
                    notification.Id,
                    notification.OrganizationAccountId);
            }
        }

        return stateChanged;
    }

    private List<AdminEmailEvent> CaptureAdminEmailEvents()
    {
        var events = new List<AdminEmailEvent>();

        events.AddRange(ChangeTracker.Entries<DonorContribution>()
            .Where(x => x.State == EntityState.Added && x.Entity.Status == "بانتظار الاعتماد")
            .Select(x => new AdminEmailEvent(AdminEmailEventKind.NewDonorContribution, x.Entity)));

        events.AddRange(ChangeTracker.Entries<DonorSurplusDecision>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => new AdminEmailEvent(AdminEmailEventKind.NewDonorSurplusDecision, x.Entity)));

        events.AddRange(ChangeTracker.Entries<OpportunityRequest>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => new AdminEmailEvent(AdminEmailEventKind.NewOpportunityRequest, x.Entity)));

        events.AddRange(ChangeTracker.Entries<OrganizationEvaluation>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => new AdminEmailEvent(AdminEmailEventKind.NewOrganizationEvaluation, x.Entity)));

        events.AddRange(ChangeTracker.Entries<ContactMessage>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => new AdminEmailEvent(AdminEmailEventKind.NewContactMessage, x.Entity)));

        events.AddRange(ChangeTracker.Entries<SatisfactionResponse>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => new AdminEmailEvent(AdminEmailEventKind.NewSatisfactionResponse, x.Entity)));

        events.AddRange(ChangeTracker.Entries<FeedbackEntry>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => new AdminEmailEvent(AdminEmailEventKind.NewFeedbackEntry, x.Entity)));

        events.AddRange(ChangeTracker.Entries<InKindDonationRequest>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => new AdminEmailEvent(AdminEmailEventKind.NewInKindDonationRequest, x.Entity)));

        events.AddRange(ChangeTracker.Entries<JobApplication>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => new AdminEmailEvent(AdminEmailEventKind.NewJobApplication, x.Entity)));

        return events;
    }

    private async Task SendAdminEventEmailsAsync(
        IReadOnlyCollection<AdminEmailEvent> events,
        NotificationEmailOptions options,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
            return;

        foreach (var adminEvent in events)
        {
            var message = await BuildAdminEmailMessageAsync(adminEvent, options, cancellationToken);
            if (message == null)
                continue;

            var recipients = await GetAdminRecipientsAsync(message.PagePath, cancellationToken);

            foreach (var admin in recipients)
            {
                try
                {
                    await _emailSender.SendNotificationAsync(
                        admin.Email!,
                        admin.FullName,
                        "لوحة تحكم الإدارة",
                        message.Title,
                        message.Message,
                        message.ActionUrl,
                        cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to email admin event {EventKind} to admin {AdminUserId}.",
                        adminEvent.Kind,
                        admin.Id);
                }
            }
        }
    }

    private async Task<AdminEmailMessage?> BuildAdminEmailMessageAsync(
        AdminEmailEvent adminEvent,
        NotificationEmailOptions options,
        CancellationToken cancellationToken)
    {
        switch (adminEvent.Kind)
        {
            case AdminEmailEventKind.NewDonorContribution:
            {
                var contribution = (DonorContribution)adminEvent.Entity;
                var donorName = await DonorAccounts.AsNoTracking()
                    .Where(x => x.Id == contribution.DonorAccountId)
                    .Select(x => x.FullName)
                    .FirstOrDefaultAsync(cancellationToken) ?? "داعم";

                return new AdminEmailMessage(
                    "/Admin/Donors",
                    "طلب دعم جديد",
                    $"أرسل {donorName} طلب دعم بعنوان: {contribution.Title}، بقيمة {contribution.TotalAmount:N2} ر.س.",
                    BuildAbsoluteUrl(options.PublicBaseUrl,
                        $"/Admin/Donors/Details/{contribution.DonorAccountId}#contribution-{contribution.Id}"));
            }

            case AdminEmailEventKind.NewDonorSurplusDecision:
            {
                var decision = (DonorSurplusDecision)adminEvent.Entity;
                var contribution = await DonorContributions.AsNoTracking()
                    .Include(x => x.DonorAccount)
                    .FirstOrDefaultAsync(x => x.Id == decision.DonorContributionId, cancellationToken);

                return new AdminEmailMessage(
                    "/Admin/Donors",
                    "رد جديد على فائض الدعم",
                    $"اختار {contribution?.DonorAccount?.FullName ?? "الداعم"}: {decision.DecisionType}، للفائض بقيمة {decision.SurplusAmount:N2} ر.س.",
                    contribution == null
                        ? BuildAbsoluteUrl(options.PublicBaseUrl, "/Admin/Donors")
                        : BuildAbsoluteUrl(options.PublicBaseUrl,
                            $"/Admin/Donors/Details/{contribution.DonorAccountId}#contribution-{contribution.Id}"));
            }

            case AdminEmailEventKind.NewOpportunityRequest:
            {
                var request = (OpportunityRequest)adminEvent.Entity;
                var organizationName = await OrganizationAccounts.AsNoTracking()
                    .Where(x => x.Id == request.OrganizationAccountId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync(cancellationToken) ?? "جهة";

                return new AdminEmailMessage(
                    "/Admin/Organizations",
                    "طلب استقطاب جديد",
                    $"أرسلت جهة {organizationName} طلب {request.OpportunityType} جديد بعنوان: {request.Title}.",
                    BuildAbsoluteUrl(options.PublicBaseUrl, $"/Admin/Organizations/Requests/{request.Id}"));
            }

            case AdminEmailEventKind.NewOrganizationEvaluation:
            {
                var evaluation = (OrganizationEvaluation)adminEvent.Entity;
                var organizationName = await OrganizationAccounts.AsNoTracking()
                    .Where(x => x.Id == evaluation.OrganizationAccountId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync(cancellationToken) ?? "جهة";

                return new AdminEmailMessage(
                    "/Admin/Organizations",
                    "تقييم خدمة جديد من جهة",
                    $"أرسلت جهة {organizationName} تقييمًا جديدًا للخدمة. تقييم الخدمة: {evaluation.ServiceRate}/5، وجودة المرشحين: {evaluation.CandidateQualityRate}/5.",
                    BuildAbsoluteUrl(options.PublicBaseUrl, $"/Admin/Organizations/Details/{evaluation.OrganizationAccountId}"));
            }

            case AdminEmailEventKind.NewContactMessage:
            {
                var item = (ContactMessage)adminEvent.Entity;
                return new AdminEmailMessage(
                    "/Admin/Messages",
                    "رسالة تواصل جديدة",
                    $"وصلت رسالة جديدة من {item.FullName} بعنوان: {item.Subject}.",
                    BuildAbsoluteUrl(options.PublicBaseUrl, $"/Admin/Messages/Details/{item.Id}"));
            }

            case AdminEmailEventKind.NewSatisfactionResponse:
            {
                var item = (SatisfactionResponse)adminEvent.Entity;
                return new AdminEmailMessage(
                    "/Admin/Satisfaction",
                    "تقييم رضا جديد",
                    $"تم استلام تقييم جديد لخدمة {item.ServiceName}: {item.Rating}/5، مستوى الرضا: {item.SatisfactionLevel}.",
                    BuildAbsoluteUrl(options.PublicBaseUrl, $"/Admin/Satisfaction/Details/{item.Id}"));
            }

            case AdminEmailEventKind.NewFeedbackEntry:
            {
                var item = (FeedbackEntry)adminEvent.Entity;
                return new AdminEmailMessage(
                    "/Admin/Feedback",
                    "تغذية راجعة جديدة",
                    $"تم استلام {item.FeedbackType} جديدة بعنوان: {item.Subject}، والتقييم {item.Rating}/5.",
                    BuildAbsoluteUrl(options.PublicBaseUrl, $"/Admin/Feedback/Details/{item.Id}"));
            }

            case AdminEmailEventKind.NewInKindDonationRequest:
            {
                var item = (InKindDonationRequest)adminEvent.Entity;
                return new AdminEmailMessage(
                    "/Admin/InKindDonationRequests",
                    "طلب تبرع عيني جديد",
                    $"أرسل {item.DonorName} طلب تبرع عيني من مدينة {item.City}. الأنواع: {item.DonationTypeNames}.",
                    BuildAbsoluteUrl(options.PublicBaseUrl, "/Admin/InKindDonationRequests"));
            }

            case AdminEmailEventKind.NewJobApplication:
            {
                var item = (JobApplication)adminEvent.Entity;
                return new AdminEmailMessage(
                    "/Admin/JobApplications",
                    "طلب توظيف جديد",
                    $"تم استلام طلب توظيف جديد من {item.FullName} للمسمى: {item.DesiredJobTitle ?? "غير محدد"}.",
                    BuildAbsoluteUrl(options.PublicBaseUrl, $"/Admin/JobApplications/Details/{item.Id}"));
            }

            default:
                return null;
        }
    }

    private async Task<IReadOnlyList<AdminUser>> GetAdminRecipientsAsync(
        string pagePath,
        CancellationToken cancellationToken)
    {
        var admins = await AdminUsers
            .AsNoTracking()
            .Where(x => x.IsActive && x.Email != null && x.Email != "")
            .ToListAsync(cancellationToken);

        admins = admins
            .Where(x => IsDeliverableEmail(x.Email))
            .OrderByDescending(x => x.IsSuperAdmin)
            .GroupBy(x => x.Email!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();

        if (admins.Count == 0)
            return Array.Empty<AdminUser>();

        var normalAdminIds = admins
            .Where(x => !x.IsSuperAdmin)
            .Select(x => x.Id)
            .ToArray();

        var administrationManagerIds = normalAdminIds.Length == 0
            ? new HashSet<int>()
            : (await AdminPagePermissions
                .AsNoTracking()
                .Where(x => normalAdminIds.Contains(x.AdminUserId) &&
                            x.PagePath == AdminRolePolicy.UserManagementPagePath &&
                            x.CanAccess)
                .Select(x => x.AdminUserId)
                .ToListAsync(cancellationToken))
                .ToHashSet();

        var permittedAdminIds = normalAdminIds.Length == 0
            ? new HashSet<int>()
            : (await AdminPagePermissions
                .AsNoTracking()
                .Where(x => normalAdminIds.Contains(x.AdminUserId) &&
                            x.PagePath == pagePath &&
                            x.CanAccess)
                .Select(x => x.AdminUserId)
                .ToListAsync(cancellationToken))
                .ToHashSet();

        return admins
            .Where(x =>
                x.IsSuperAdmin ||
                administrationManagerIds.Contains(x.Id) ||
                permittedAdminIds.Contains(x.Id))
            .ToList();
    }

    private static bool IsDeliverableEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        email = email.Trim();
        if (email.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var address = new System.Net.Mail.MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? BuildAbsoluteUrl(string? publicBaseUrl, string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttps || absolute.Scheme == Uri.UriSchemeHttp))
        {
            return absolute.ToString();
        }

        if (string.IsNullOrWhiteSpace(publicBaseUrl))
            return null;

        if (!Uri.TryCreate(publicBaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp))
        {
            return null;
        }

        return new Uri(baseUri, path.TrimStart('/')).ToString();
    }

    private enum AdminEmailEventKind
    {
        NewDonorContribution,
        NewDonorSurplusDecision,
        NewOpportunityRequest,
        NewOrganizationEvaluation,
        NewContactMessage,
        NewSatisfactionResponse,
        NewFeedbackEntry,
        NewInKindDonationRequest,
        NewJobApplication
    }

    private sealed record AdminEmailEvent(AdminEmailEventKind Kind, object Entity);

    private sealed record AdminEmailMessage(
        string PagePath,
        string Title,
        string Message,
        string? ActionUrl);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DonorContribution>()
            .HasIndex(x => x.ContributionCode)
            .IsUnique();

        modelBuilder.Entity<DonorContributionCertificate>()
            .HasIndex(x => x.CertificateNumber)
            .IsUnique();

        modelBuilder.Entity<DonorContributionCertificate>()
            .HasIndex(x => x.DonorContributionId)
            .IsUnique();

        modelBuilder.Entity<DonorContributionCertificate>()
            .HasOne(x => x.DonorContribution)
            .WithOne(x => x.Certificate)
            .HasForeignKey<DonorContributionCertificate>(x => x.DonorContributionId)
            .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<PasswordSetupToken>()
            .HasIndex(x => x.TokenHash)
            .IsUnique();

        modelBuilder.Entity<PasswordSetupToken>()
            .HasIndex(x => new { x.AccountType, x.AccountId, x.ExpiresAtUtc });

        modelBuilder.Entity<SystemLog>()
            .HasIndex(x => x.CreatedAt);

        modelBuilder.Entity<SystemLog>()
            .HasIndex(x => new { x.EventType, x.Success });

        modelBuilder.Entity<LoginOtpChallenge>()
            .HasIndex(x => new { x.Email, x.LastSentAtUtc });

        modelBuilder.Entity<LoginOtpChallenge>()
            .HasIndex(x => new { x.PortalType, x.AccountId, x.ExpiresAtUtc });

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DonorAccount>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<OrganizationAccount>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<DonorContribution>()
            .Property(x => x.TotalAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<DonorContribution>()
            .Property(x => x.SpentAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<DonorContribution>()
            .Property(x => x.RemainingAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<DonorSurplusDecision>()
            .Property(x => x.SurplusAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<DonorContributionCertificate>()
            .Property(x => x.TotalAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<DonorContributionCertificate>()
            .Property(x => x.SpentAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<DonorContributionCertificate>()
            .Property(x => x.RemainingAmount)
            .HasColumnType("decimal(18,2)");
    }
}


