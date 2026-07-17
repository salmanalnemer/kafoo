using Kafo.Web.Configuration;

namespace Kafo.Web.Security;

public static class LoginSecurity
{
    private static readonly int[] DefaultScheduleMinutes = [1, 2, 5, 15];

    public static bool IsLocked(DateTime? lockoutEndUtc)
        => lockoutEndUtc.HasValue && lockoutEndUtc.Value > DateTime.UtcNow;

    public static TimeSpan? GetRemainingLockout(DateTime? lockoutEndUtc)
    {
        if (!lockoutEndUtc.HasValue)
            return null;

        var remaining = lockoutEndUtc.Value - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : null;
    }

    public static TimeSpan? RegisterFailure(
        ref int accessFailedCount,
        ref DateTime? lockoutEndUtc,
        SecurityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return RegisterFailure(
            ref accessFailedCount,
            ref lockoutEndUtc,
            options.LoginMaxFailures,
            options.LoginLockoutScheduleMinutes,
            options.LoginFailureResetMinutes,
            options.LoginLockoutMinutes);
    }

    public static TimeSpan? RegisterFailure(
        ref int accessFailedCount,
        ref DateTime? lockoutEndUtc,
        int maxFailures,
        IReadOnlyList<int>? scheduleMinutes,
        int resetAfterMinutes = 30,
        int fallbackMaximumMinutes = 15)
    {
        var now = DateTime.UtcNow;
        var normalizedMaxFailures = Math.Max(3, maxFailures);
        var normalizedResetMinutes = Math.Clamp(resetAfterMinutes, 5, 24 * 60);

        if (lockoutEndUtc.HasValue && lockoutEndUtc.Value <= now)
        {
            var elapsedAfterExpiry = now - lockoutEndUtc.Value;
            if (elapsedAfterExpiry >= TimeSpan.FromMinutes(normalizedResetMinutes))
                accessFailedCount = 0;

            lockoutEndUtc = null;
        }

        accessFailedCount = Math.Max(0, accessFailedCount) + 1;

        if (accessFailedCount < normalizedMaxFailures)
            return null;

        var schedule = NormalizeSchedule(scheduleMinutes, fallbackMaximumMinutes);
        var stageIndex = Math.Min(
            accessFailedCount - normalizedMaxFailures,
            schedule.Length - 1);

        var duration = TimeSpan.FromMinutes(schedule[stageIndex]);
        lockoutEndUtc = now.Add(duration);

        return duration;
    }

    // Backward-compatible overload for any older callers.
    public static TimeSpan? RegisterFailure(
        ref int accessFailedCount,
        ref DateTime? lockoutEndUtc,
        int maxFailures,
        int lockoutMinutes)
        => RegisterFailure(
            ref accessFailedCount,
            ref lockoutEndUtc,
            maxFailures,
            DefaultScheduleMinutes,
            resetAfterMinutes: 30,
            fallbackMaximumMinutes: lockoutMinutes);

    public static void Reset(
        ref int accessFailedCount,
        ref DateTime? lockoutEndUtc)
    {
        accessFailedCount = 0;
        lockoutEndUtc = null;
    }

    public static string NewSecurityStamp()
        => Guid.NewGuid().ToString("N");

    private static int[] NormalizeSchedule(
        IReadOnlyList<int>? configuredSchedule,
        int fallbackMaximumMinutes)
    {
        var fallbackMaximum = Math.Clamp(fallbackMaximumMinutes, 1, 24 * 60);

        var normalized = configuredSchedule?
            .Where(value => value > 0)
            .Select(value => Math.Clamp(value, 1, 24 * 60))
            .Take(10)
            .ToArray();

        if (normalized is not { Length: > 0 })
            normalized = [1, 2, 5, fallbackMaximum];

        for (var index = 1; index < normalized.Length; index++)
        {
            if (normalized[index] < normalized[index - 1])
                normalized[index] = normalized[index - 1];
        }

        return normalized;
    }
}
