using System.Security.Cryptography;
using System.Text;
using Kafo.Web.Configuration;
using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Security;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kafo.Web.Services.Implementations;

public sealed class PasswordSetupService : IPasswordSetupService
{
    private const string ProductionPublicBaseUrl = "https://app1.kafoo.org.sa";

    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly SecurityOptions _options;

    public PasswordSetupService(
        ApplicationDbContext db,
        IEmailSender emailSender,
        IOptions<SecurityOptions> options)
    {
        _db = db;
        _emailSender = emailSender;
        _options = options.Value;
    }

    public async Task<string> CreateTokenAsync(
        string accountType,
        int accountId,
        int? requestedByAdminUserId,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var result = await CreateTokenStateAsync(
            accountType,
            accountId,
            requestedByAdminUserId,
            httpContext,
            cancellationToken);

        return result.RawToken;
    }

    public async Task IssueAsync(
        string accountType,
        int accountId,
        string recipientEmail,
        string recipientName,
        string accountLabel,
        int? requestedByAdminUserId,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (!PortalEmailPolicy.IsDeliverable(recipientEmail))
            throw new InvalidOperationException("البريد الإلكتروني غير صالح لاستقبال رابط إعداد كلمة المرور.");

        var result = await CreateTokenStateAsync(
            accountType,
            accountId,
            requestedByAdminUserId,
            httpContext,
            cancellationToken);

        var baseUrl = ResolvePublicBaseUrl(_options.PublicBaseUrl);
        var actionUrl = $"{baseUrl}/Account/SetPassword?token={Uri.EscapeDataString(result.RawToken)}";
        try
        {
            await _emailSender.SendNotificationAsync(
                recipientEmail,
                recipientName,
                accountLabel,
                "إعداد كلمة المرور",
                $"تم إنشاء رابط آمن لإعداد كلمة مرور حسابك. الرابط صالح لمدة {Math.Clamp(_options.PasswordSetupTokenMinutes, 15, 120)} دقيقة ولمرة واحدة فقط.",
                actionUrl,
                cancellationToken);
        }
        catch
        {
            _db.PasswordSetupTokens.Remove(result.Token);
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    private static string ResolvePublicBaseUrl(string? configuredValue)
    {
        var candidate = (configuredValue ?? string.Empty).Trim().TrimEnd('/');

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            uri.Host.EndsWith(".trycloudflare.com", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Contains("your-tunnel", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return ProductionPublicBaseUrl;
        }

        // روابط إعداد كلمة المرور لهذا النظام يجب أن تستخدم نطاق الإنتاج الرسمي فقط.
        return string.Equals(
            uri.Host,
            "app1.kafoo.org.sa",
            StringComparison.OrdinalIgnoreCase)
                ? $"{uri.Scheme}://{uri.Authority}"
                : ProductionPublicBaseUrl;
    }

    public static string HashToken(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();

    public static string NormalizeAccountType(string accountType)
        => accountType.Trim().ToLowerInvariant() switch
        {
            "admin" => "Admin",
            "donor" => "Donor",
            "organization" => "Organization",
            _ => throw new InvalidOperationException("نوع الحساب غير صالح.")
        };

    private async Task<(string RawToken, PasswordSetupToken Token)> CreateTokenStateAsync(
        string accountType,
        int accountId,
        int? requestedByAdminUserId,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var normalizedType = NormalizeAccountType(accountType);
        var now = DateTime.UtcNow;

        var existing = await _db.PasswordSetupTokens
            .Where(x =>
                x.AccountType == normalizedType &&
                x.AccountId == accountId &&
                x.UsedAtUtc == null &&
                x.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);

        foreach (var item in existing)
            item.UsedAtUtc = now;

        var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var token = new PasswordSetupToken
        {
            AccountType = normalizedType,
            AccountId = accountId,
            TokenHash = HashToken(rawToken),
            ExpiresAtUtc = now.AddMinutes(Math.Clamp(_options.PasswordSetupTokenMinutes, 15, 120)),
            RequestedByAdminUserId = requestedByAdminUserId,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            CreatedAtUtc = now
        };

        _db.PasswordSetupTokens.Add(token);
        await _db.SaveChangesAsync(cancellationToken);
        return (rawToken, token);
    }
}
