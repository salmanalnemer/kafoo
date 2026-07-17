using System.Net;
using System.Text;
using Kafo.Web.Configuration;
using Kafo.Web.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using MimeKit.Utils;

namespace Kafo.Web.Services.Implementations;

public sealed class SmtpEmailSender : IEmailSender
{
    private const string DefaultSenderName = "جمعية كفو لتمكين ذوي الإعاقة";
    private const string DefaultSubject = "رسالة من جمعية كفو";

    private readonly IOptionsMonitor<SmtpEmailOptions> _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptionsMonitor<SmtpEmailOptions> options,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendLoginOtpAsync(
        string recipientEmail,
        string recipientName,
        string code,
        TimeSpan validity,
        CancellationToken cancellationToken = default)
    {
        ValidateOtpCode(code);

        var minutes = Math.Max(1, (int)Math.Ceiling(validity.TotalMinutes));
        var safeName = EncodeName(recipientName);
        var safeCode = WebUtility.HtmlEncode(code.Trim());

        var html = $"""
            <!doctype html>
            <html lang="ar" dir="rtl">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <title>التحقق من تسجيل الدخول</title>
            </head>
            <body style="margin:0;background:#f6f7fb;font-family:Tahoma,Arial,sans-serif;color:#26384d">
              <div style="max-width:620px;margin:32px auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:22px;overflow:hidden">
                <div style="background:#74429a;padding:26px 30px;color:#ffffff">
                  <h1 style="margin:0;font-size:23px">التحقق من تسجيل الدخول</h1>
                </div>
                <div style="padding:30px;line-height:1.9">
                  <p style="margin-top:0">مرحبًا {safeName}،</p>
                  <p>استخدم رمز التحقق التالي لإكمال تسجيل الدخول إلى بوابة جمعية كفو:</p>
                  <div dir="ltr" style="margin:24px 0;text-align:center;font-size:34px;font-weight:800;letter-spacing:10px;color:#74429a;background:#f7f2fb;border:1px dashed #bba0cf;border-radius:16px;padding:18px">{safeCode}</div>
                  <p>الرمز صالح لمدة <strong>{minutes} دقائق</strong> ولمرة واحدة فقط.</p>
                  <p style="color:#667085;font-size:14px">إذا لم تطلب تسجيل الدخول، تجاهل هذه الرسالة ولا تشارك الرمز مع أي شخص.</p>
                </div>
                <div style="padding:18px 30px;background:#f8fafc;color:#667085;font-size:13px">جمعية كفو لتمكين ذوي الإعاقة</div>
              </div>
            </body>
            </html>
            """;

        return SendHtmlAsync(
            recipientEmail,
            recipientName,
            "رمز التحقق من تسجيل الدخول | جمعية كفو",
            html,
            "login OTP",
            cancellationToken);
    }

    public Task SendNotificationAsync(
        string recipientEmail,
        string recipientName,
        string portalName,
        string title,
        string message,
        string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        var safeName = EncodeName(recipientName);
        var safePortalName = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(portalName)
                ? "منصة جمعية كفو"
                : portalName.Trim());

        var normalizedTitle = NormalizeHeaderText(
            title,
            "إشعار جديد",
            maxLength: 120,
            fallbackOnMojibake: false);

        var safeTitle = WebUtility.HtmlEncode(normalizedTitle);

        var safeMessage = WebUtility.HtmlEncode(message?.Trim() ?? string.Empty)
            .Replace("\r\n", "<br>", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal);

        var actionButton = BuildSafeActionButton(actionUrl);

        var html = $"""
            <!doctype html>
            <html lang="ar" dir="rtl">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <title>إشعار جديد</title>
            </head>
            <body style="margin:0;background:#f6f7fb;font-family:Tahoma,Arial,sans-serif;color:#26384d">
              <div style="max-width:650px;margin:32px auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:22px;overflow:hidden">
                <div style="background:#74429a;padding:26px 30px;color:#ffffff">
                  <div style="font-size:14px;opacity:.9;margin-bottom:7px">{safePortalName}</div>
                  <h1 style="margin:0;font-size:23px">إشعار جديد</h1>
                </div>
                <div style="padding:30px;line-height:1.9">
                  <p style="margin-top:0">مرحبًا {safeName}،</p>
                  <div style="background:#f7f2fb;border:1px solid #dfd1ea;border-radius:18px;padding:20px;margin:20px 0">
                    <h2 style="margin:0 0 12px;color:#74429a;font-size:20px">{safeTitle}</h2>
                    <div style="font-size:16px;color:#344054">{safeMessage}</div>
                  </div>
                  {actionButton}
                  <p style="color:#667085;font-size:14px">هذه الرسالة أُرسلت تلقائيًا لأن لديك إشعارًا جديدًا في النظام.</p>
                </div>
                <div style="padding:18px 30px;background:#f8fafc;color:#667085;font-size:13px">جمعية كفو لتمكين ذوي الإعاقة</div>
              </div>
            </body>
            </html>
            """;

        return SendHtmlAsync(
            recipientEmail,
            recipientName,
            $"إشعار جديد: {normalizedTitle} | جمعية كفو",
            html,
            "system notification",
            cancellationToken);
    }

    private async Task SendHtmlAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string html,
        string operation,
        CancellationToken cancellationToken)
    {
        var settings = _options.CurrentValue;
        var validatedSettings = ValidateSettings(settings);
        var recipientAddress = ValidateEmailAddress(
            recipientEmail,
            "البريد الإلكتروني للمستلم غير صحيح.");

        var senderName = NormalizeHeaderText(
            settings.FromName,
            DefaultSenderName,
            maxLength: 100,
            fallbackOnMojibake: true);

        var safeRecipientName = NormalizeHeaderText(
            recipientName,
            string.Empty,
            maxLength: 100,
            fallbackOnMojibake: false);

        var safeSubject = NormalizeHeaderText(
            subject,
            DefaultSubject,
            maxLength: 180,
            fallbackOnMojibake: true);

        if (string.IsNullOrWhiteSpace(html))
            throw new InvalidOperationException("محتوى رسالة البريد الإلكتروني فارغ.");

        var message = new MimeMessage
        {
            Date = DateTimeOffset.Now,
            MessageId = MimeUtils.GenerateMessageId(),
            Subject = safeSubject
        };

        // تحديد UTF-8 صراحةً يمنع تشوه الاسم العربي في ترويسات البريد.
        message.From.Add(new MailboxAddress(
            Encoding.UTF8,
            senderName,
            validatedSettings.FromAddress));

        message.To.Add(new MailboxAddress(
            Encoding.UTF8,
            safeRecipientName,
            recipientAddress));

        var body = new TextPart(TextFormat.Html)
        {
            Text = html
        };
        body.ContentType.Charset = "utf-8";
        message.Body = body;

        using var client = new SmtpClient
        {
            Timeout = Math.Clamp(settings.TimeoutSeconds, 5, 120) * 1000,
            CheckCertificateRevocation = true
        };

        try
        {
            await client.ConnectAsync(
                validatedSettings.Host,
                settings.Port,
                ResolveSocketOptions(settings),
                cancellationToken);

            if (!client.IsSecure)
            {
                throw new InvalidOperationException(
                    "تعذر إنشاء اتصال SMTP مشفر وآمن.");
            }

            // النظام يستخدم اسم مستخدم وكلمة مرور؛ لا نعلن دعم OAuth2 هنا.
            client.AuthenticationMechanisms.Remove("XOAUTH2");

            await client.AuthenticateAsync(
                validatedSettings.UserName,
                settings.Password,
                cancellationToken);

            await client.SendAsync(message, cancellationToken);

            _logger.LogInformation(
                "SMTP {Operation} email sent successfully to {RecipientEmail} through {SmtpHost}:{SmtpPort}",
                operation,
                MaskEmail(recipientAddress),
                validatedSettings.Host,
                settings.Port);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send SMTP {Operation} email to {RecipientEmail} through {SmtpHost}:{SmtpPort}. TLS={EnableSsl}",
                operation,
                MaskEmail(recipientAddress),
                validatedSettings.Host,
                settings.Port,
                settings.EnableSsl);

            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                try
                {
                    await client.DisconnectAsync(
                        quit: true,
                        CancellationToken.None);
                }
                catch (Exception disconnectException)
                {
                    _logger.LogWarning(
                        disconnectException,
                        "SMTP disconnect failed after {Operation}.",
                        operation);
                }
            }
        }
    }

    private static string BuildSafeActionButton(string? actionUrl)
    {
        if (string.IsNullOrWhiteSpace(actionUrl) ||
            !Uri.TryCreate(actionUrl.Trim(), UriKind.Absolute, out var parsedUrl) ||
            !string.Equals(
                parsedUrl.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var safeActionUrl = WebUtility.HtmlEncode(parsedUrl.AbsoluteUri);

        return $"""
            <p style="text-align:center;margin:28px 0 8px">
              <a href="{safeActionUrl}" rel="noopener noreferrer" style="display:inline-block;background:#4db748;color:#ffffff;text-decoration:none;font-weight:800;padding:13px 24px;border-radius:13px">فتح الخدمة في النظام</a>
            </p>
            """;
    }

    private static string EncodeName(string? recipientName)
        => WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(recipientName)
                ? "مستخدم المنصة"
                : recipientName.Trim());

    private static SecureSocketOptions ResolveSocketOptions(
        SmtpEmailOptions settings)
    {
        if (!settings.EnableSsl)
        {
            throw new InvalidOperationException(
                "يجب تفعيل SSL/TLS في إعدادات SMTP.");
        }

        return settings.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }

    private static ValidatedSmtpSettings ValidateSettings(
        SmtpEmailOptions settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(settings.Host) ||
            string.IsNullOrWhiteSpace(settings.UserName) ||
            string.IsNullOrWhiteSpace(settings.Password) ||
            string.IsNullOrWhiteSpace(settings.FromEmail))
        {
            throw new InvalidOperationException(
                "إعدادات SMTP غير مكتملة. اضبط Email:Smtp في إعدادات البيئة أو User Secrets.");
        }

        if (settings.Port is < 1 or > 65535)
            throw new InvalidOperationException("منفذ SMTP غير صحيح.");

        var host = NormalizeHeaderText(
            settings.Host,
            string.Empty,
            maxLength: 255,
            fallbackOnMojibake: false);

        if (string.IsNullOrWhiteSpace(host) ||
            Uri.CheckHostName(host) == UriHostNameType.Unknown)
        {
            throw new InvalidOperationException("عنوان خادم SMTP غير صحيح.");
        }

        var userName = NormalizeCredentialValue(
            settings.UserName,
            "اسم مستخدم SMTP غير صحيح.");

        var fromAddress = ValidateEmailAddress(
            settings.FromEmail,
            "عنوان البريد المرسل غير صحيح.");

        if (!settings.EnableSsl)
        {
            throw new InvalidOperationException(
                "الاتصال غير المشفر بخادم SMTP غير مسموح.");
        }

        return new ValidatedSmtpSettings(
            host,
            userName,
            fromAddress);
    }

    private static string ValidateEmailAddress(
        string? value,
        string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(errorMessage);

        var trimmed = value.Trim();

        if (ContainsHeaderControlCharacters(trimmed) ||
            !MailboxAddress.TryParse(trimmed, out var mailbox) ||
            !string.Equals(
                mailbox.Address,
                trimmed,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return mailbox.Address;
    }

    private static string NormalizeCredentialValue(
        string? value,
        string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(errorMessage);

        var trimmed = value.Trim();

        if (ContainsHeaderControlCharacters(trimmed))
            throw new InvalidOperationException(errorMessage);

        return trimmed;
    }

    private static string NormalizeHeaderText(
        string? value,
        string fallback,
        int maxLength,
        bool fallbackOnMojibake)
    {
        var source = string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();

        if (fallbackOnMojibake && LooksLikeMojibake(source))
            source = fallback;

        var builder = new StringBuilder(source.Length);
        var previousWasSpace = false;

        foreach (var character in source.Normalize(NormalizationForm.FormC))
        {
            if (char.IsControl(character) || IsBidirectionalControl(character))
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                    previousWasSpace = true;
                }

                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                    previousWasSpace = true;
                }

                continue;
            }

            builder.Append(character);
            previousWasSpace = false;
        }

        var normalized = builder.ToString().Trim();

        if (string.IsNullOrWhiteSpace(normalized))
            normalized = fallback;

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }

    private static bool ContainsHeaderControlCharacters(string value)
        => value.IndexOfAny(['\r', '\n', '\0']) >= 0;

    private static bool IsBidirectionalControl(char value)
        => value is >= '\u202A' and <= '\u202E'
            or >= '\u2066' and <= '\u2069';

    private static bool LooksLikeMojibake(string value)
    {
        if (value.Contains('\uFFFD') ||
            value.Contains("Ø", StringComparison.Ordinal) ||
            value.Contains("Ù", StringComparison.Ordinal) ||
            value.Contains("Ã", StringComparison.Ordinal) ||
            value.Contains("Â", StringComparison.Ordinal))
        {
            return true;
        }

        var suspiciousArabicCharacters = 0;
        foreach (var character in value)
        {
            if (character is 'ط' or 'ظ')
                suspiciousArabicCharacters++;
        }

        return value.Length >= 8 &&
               suspiciousArabicCharacters >= Math.Max(4, value.Length / 5);
    }

    private static void ValidateOtpCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("رمز التحقق غير موجود.");

        var trimmed = code.Trim();

        if (trimmed.Length is < 4 or > 10 ||
            trimmed.Any(character => !char.IsDigit(character)))
        {
            throw new InvalidOperationException("صيغة رمز التحقق غير صحيحة.");
        }
    }

    private static string MaskEmail(string emailAddress)
    {
        var separatorIndex = emailAddress.IndexOf('@');

        if (separatorIndex <= 1)
            return "***";

        var localPart = emailAddress[..separatorIndex];
        var domainPart = emailAddress[(separatorIndex + 1)..];

        return $"{localPart[0]}***@{domainPart}";
    }

    private sealed record ValidatedSmtpSettings(
        string Host,
        string UserName,
        string FromAddress);
}