using Kafo.Web.Models;
using Kafo.Web.Security;
using Kafo.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        var missingContributionCodes = await db.DonorContributions
            .Where(x => x.ContributionCode == null || x.ContributionCode == "")
            .OrderBy(x => x.Id)
            .ToListAsync();

        foreach (var contribution in missingContributionCodes)
        {
            contribution.ContributionCode = DonorContributionCodeGenerator.Generate(db);
            contribution.UpdatedAt = DateTime.Now;
        }

        if (missingContributionCodes.Count > 0)
            await db.SaveChangesAsync();

        if (await db.AdminUsers.AnyAsync())
            return;

        var email = configuration["BootstrapAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["BootstrapAdmin:Password"];
        var userName = configuration["BootstrapAdmin:UserName"]?.Trim();
        var fullName = configuration["BootstrapAdmin:FullName"]?.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(userName))
        {
            logger.LogWarning(
                "No administrator exists. Set BootstrapAdmin__Email, BootstrapAdmin__UserName and BootstrapAdmin__Password once, start the application, then remove the password from the environment.");
            return;
        }

        var passwordErrors = PasswordPolicy.Validate(password);
        if (passwordErrors.Count > 0)
            throw new InvalidOperationException(
                "Bootstrap administrator password does not meet the password policy: " +
                string.Join(" ", passwordErrors));

        if (!PortalEmailPolicy.IsDeliverable(email))
            throw new InvalidOperationException("Bootstrap administrator email is invalid.");

        var hashed = AdminPasswordHasher.HashPassword(password);
        var admin = new AdminUser
        {
            FullName = string.IsNullOrWhiteSpace(fullName) ? "مدير النظام" : fullName,
            UserName = userName,
            Email = email,
            PasswordHash = hashed.Hash,
            PasswordSalt = hashed.Salt,
            SecurityStamp = LoginSecurity.NewSecurityStamp(),
            PasswordChangedAtUtc = DateTime.UtcNow,
            MustChangePassword = false,
            IsSuperAdmin = true,
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync();
        logger.LogWarning(
            "Bootstrap administrator {AdminId} was created. Remove BootstrapAdmin__Password from the environment now.",
            admin.Id);
    }
}
