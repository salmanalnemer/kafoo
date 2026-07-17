using System.Security.Cryptography;
using Kafo.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Services;

public static class DonorContributionCodeGenerator
{
    public static string Generate(ApplicationDbContext context)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var number = RandomNumberGenerator.GetInt32(0, 1_000_000);
            var code = $"KAFD{number:D6}";

            var exists = context.DonorContributions
                .AsNoTracking()
                .Any(x => x.ContributionCode == code);

            if (!exists)
                return code;
        }

        throw new InvalidOperationException("Unable to generate a unique donor contribution code.");
    }
}
