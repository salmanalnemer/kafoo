using System.Security.Cryptography;
using Kafo.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Services;

public static class DonorCertificateNumberGenerator
{
    public static async Task<string> GenerateAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 80; attempt++)
        {
            var number = RandomNumberGenerator.GetInt32(0, 100_000_000);
            var certificateNumber = $"kafo-b-{number:D8}";

            var exists = await context.DonorContributionCertificates
                .AsNoTracking()
                .AnyAsync(x => x.CertificateNumber == certificateNumber, cancellationToken);

            if (!exists)
                return certificateNumber;
        }

        throw new InvalidOperationException("Unable to generate a unique donor certificate number.");
    }
}
