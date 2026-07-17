namespace Kafo.Web.Services.Interfaces;

public interface IEmailSender
{
    Task SendLoginOtpAsync(
        string recipientEmail,
        string recipientName,
        string code,
        TimeSpan validity,
        CancellationToken cancellationToken = default);

    Task SendNotificationAsync(
        string recipientEmail,
        string recipientName,
        string portalName,
        string title,
        string message,
        string? actionUrl = null,
        CancellationToken cancellationToken = default);
}
