using System.Text.RegularExpressions;

namespace Kafo.Web.Security;

public static partial class PasswordPolicy
{
    public const int MinimumLength = 12;
    public const int MaximumLength = 128;

    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "password123", "password@123", "admin123", "admin@123",
        "qwerty123", "123456789", "1234567890", "welcome123", "letmein123",
        "kafo123456", "kafo@12345"
    };

    public static IReadOnlyList<string> Validate(string? password)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("كلمة المرور مطلوبة.");
            return errors;
        }

        if (password.Length < MinimumLength)
            errors.Add($"يجب ألا تقل كلمة المرور عن {MinimumLength} حرفًا.");
        if (password.Length > MaximumLength)
            errors.Add($"يجب ألا تزيد كلمة المرور عن {MaximumLength} حرفًا.");
        if (CommonPasswords.Contains(password) || password.Contains("123456", StringComparison.OrdinalIgnoreCase))
            errors.Add("كلمة المرور شائعة أو سهلة التخمين؛ اختر عبارة أطول وغير متوقعة.");
        if (!UppercaseRegex().IsMatch(password))
            errors.Add("يجب أن تحتوي كلمة المرور على حرف إنجليزي كبير واحد على الأقل.");
        if (!LowercaseRegex().IsMatch(password))
            errors.Add("يجب أن تحتوي كلمة المرور على حرف إنجليزي صغير واحد على الأقل.");
        if (!DigitRegex().IsMatch(password))
            errors.Add("يجب أن تحتوي كلمة المرور على رقم واحد على الأقل.");
        if (!SymbolRegex().IsMatch(password))
            errors.Add("يجب أن تحتوي كلمة المرور على رمز خاص واحد على الأقل.");
        if (WhitespaceRegex().IsMatch(password))
            errors.Add("لا يسمح بالمسافات داخل كلمة المرور.");

        return errors;
    }

    public static bool IsValid(string? password) => Validate(password).Count == 0;

    [GeneratedRegex("[A-Z]", RegexOptions.CultureInvariant)]
    private static partial Regex UppercaseRegex();

    [GeneratedRegex("[a-z]", RegexOptions.CultureInvariant)]
    private static partial Regex LowercaseRegex();

    [GeneratedRegex("[0-9]", RegexOptions.CultureInvariant)]
    private static partial Regex DigitRegex();

    [GeneratedRegex("[^A-Za-z0-9]", RegexOptions.CultureInvariant)]
    private static partial Regex SymbolRegex();

    [GeneratedRegex("\\s", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
