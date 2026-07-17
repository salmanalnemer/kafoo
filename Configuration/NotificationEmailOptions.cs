namespace Kafo.Web.Configuration;

public sealed class NotificationEmailOptions
{
    public const string SectionName = "Email:Notifications";

    public bool Enabled { get; set; } = true;

    public bool SendPortalNotifications { get; set; } = true;

    public bool SendAdminNotifications { get; set; } = true;

    public string PublicBaseUrl { get; set; } = string.Empty;
}
