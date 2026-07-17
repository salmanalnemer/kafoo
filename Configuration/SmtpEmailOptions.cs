namespace Kafo.Web.Configuration;

public sealed class SmtpEmailOptions
{
    public const string SectionName = "Email:Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "support@kafoo.com.sa";
    public string FromName { get; set; } = "جمعية كفو لتمكين ذوي الإعاقة";
    public int TimeoutSeconds { get; set; } = 30;
}
