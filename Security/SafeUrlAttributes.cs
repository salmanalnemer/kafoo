using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Security;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class HttpsUrlAttribute : ValidationAttribute
{
    public HttpsUrlAttribute()
        : base("يجب أن يكون الرابط كاملاً ويستخدم HTTPS.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        if (value is not string text) return false;
        if (string.IsNullOrWhiteSpace(text)) return true;

        return Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri) &&
               uri.Scheme == Uri.UriSchemeHttps &&
               !string.IsNullOrWhiteSpace(uri.Host) &&
               string.IsNullOrEmpty(uri.UserInfo);
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class SafeNavigationUrlAttribute : ValidationAttribute
{
    public SafeNavigationUrlAttribute()
        : base("الرابط غير آمن. استخدم مسارًا داخليًا يبدأ بـ / أو رابط HTTPS كاملاً.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        if (value is not string text) return false;
        text = text.Trim();
        if (text.Length == 0 || text == "#") return true;

        if (text.StartsWith("/", StringComparison.Ordinal) &&
            !text.StartsWith("//", StringComparison.Ordinal) &&
            !text.Contains('\\') &&
            !text.Any(char.IsControl))
        {
            return true;
        }

        return Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
               uri.Scheme == Uri.UriSchemeHttps &&
               !string.IsNullOrWhiteSpace(uri.Host) &&
               string.IsNullOrEmpty(uri.UserInfo);
    }
}
