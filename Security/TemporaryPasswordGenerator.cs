using System.Security.Cryptography;

namespace Kafo.Web.Security;

public static class TemporaryPasswordGenerator
{
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%*-_";
    private static readonly string AllCharacters = Uppercase + Lowercase + Digits + Symbols;

    public static string Generate(int length = 14)
    {
        if (length < 10)
            throw new ArgumentOutOfRangeException(nameof(length), "طول كلمة المرور المؤقتة يجب ألا يقل عن 10 أحرف.");

        var characters = new List<char>(length)
        {
            Pick(Uppercase),
            Pick(Lowercase),
            Pick(Digits),
            Pick(Symbols)
        };

        while (characters.Count < length)
            characters.Add(Pick(AllCharacters));

        for (var i = characters.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (characters[i], characters[j]) = (characters[j], characters[i]);
        }

        return new string(characters.ToArray());
    }

    private static char Pick(string source)
        => source[RandomNumberGenerator.GetInt32(source.Length)];
}
