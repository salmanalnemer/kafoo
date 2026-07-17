namespace Kafo.Web.Configuration;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public string PublicBaseUrl { get; set; } = "https://kafoo.org.sa";
    public string PrivateStoragePath { get; set; } = "App_Data/secure-uploads";
    public string DataProtectionKeysPath { get; set; } = "App_Data/data-protection-keys";
    public string LogPath { get; set; } = "App_Data/logs";

    /// <summary>
    /// Number of consecutive failed password attempts before the first temporary lockout.
    /// </summary>
    public int LoginMaxFailures { get; set; } = 5;

    /// <summary>
    /// Progressive lockout schedule in minutes. Default: 1, 2, 5, then 15 minutes.
    /// The final value is reused for later consecutive failures.
    /// </summary>
    public int[] LoginLockoutScheduleMinutes { get; set; } = [1, 2, 5, 15];

    /// <summary>
    /// If no additional failed attempt occurs for this many minutes after a lockout expires,
    /// the progressive failure counter is reset.
    /// </summary>
    public int LoginFailureResetMinutes { get; set; } = 30;

    /// <summary>
    /// Kept for backward-compatible configuration. It is used as the fallback maximum
    /// lockout duration when LoginLockoutScheduleMinutes is missing or invalid.
    /// </summary>
    public int LoginLockoutMinutes { get; set; } = 15;

    public int PasswordSetupTokenMinutes { get; set; } = 30;
}
