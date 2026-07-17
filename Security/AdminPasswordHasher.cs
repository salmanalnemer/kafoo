using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Kafo.Web.Security;

public static class AdminPasswordHasher
{
    private const int LegacyIterations = 120_000;
    private const int LegacySaltSize = 16;
    private const int LegacyKeySize = 32;
    private const string IdentityMarker = "IDENTITY_V3";

    private static readonly PasswordHasher<object> IdentityHasher = new(
        Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
            IterationCount = 600_000
        }));

    private static readonly string DummyHash = IdentityHasher.HashPassword(new object(), "Dummy-Password-Only-For-Timing!123");

    public static (string Hash, string Salt) HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return (IdentityHasher.HashPassword(new object(), password), IdentityMarker);
    }

    public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        => VerifyPassword(password, storedHash, storedSalt, out _);

    public static bool VerifyPassword(
        string password,
        string storedHash,
        string storedSalt,
        out bool needsRehash)
    {
        needsRehash = false;
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            VerifyDummy(password ?? string.Empty);
            return false;
        }

        try
        {
            if (string.Equals(storedSalt, IdentityMarker, StringComparison.Ordinal))
            {
                var result = IdentityHasher.VerifyHashedPassword(new object(), storedHash, password);
                needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
                return result != PasswordVerificationResult.Failed;
            }

            if (string.IsNullOrWhiteSpace(storedSalt))
            {
                VerifyDummy(password);
                return false;
            }

            var saltBytes = Convert.FromBase64String(storedSalt);
            var expectedHash = Convert.FromBase64String(storedHash);
            var computed = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                LegacyIterations,
                HashAlgorithmName.SHA256,
                LegacyKeySize);

            var valid = expectedHash.Length == computed.Length &&
                        CryptographicOperations.FixedTimeEquals(expectedHash, computed);
            needsRehash = valid;
            return valid;
        }
        catch (FormatException)
        {
            VerifyDummy(password);
            return false;
        }
        catch (CryptographicException)
        {
            VerifyDummy(password);
            return false;
        }
    }

    public static void VerifyDummy(string password)
        => _ = IdentityHasher.VerifyHashedPassword(new object(), DummyHash, password ?? string.Empty);
}
