using Kafo.Web.Data;
using Kafo.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Services.Implementations;

public sealed class LegacyPrivateFileMigrationService
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly IFileUploadService _files;
    private readonly ILogger<LegacyPrivateFileMigrationService> _logger;
    private readonly Dictionary<string, string> _migrated = new(StringComparer.OrdinalIgnoreCase);

    public LegacyPrivateFileMigrationService(
        ApplicationDbContext db,
        IWebHostEnvironment environment,
        IFileUploadService files,
        ILogger<LegacyPrivateFileMigrationService> logger)
    {
        _db = db;
        _environment = environment;
        _files = files;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        var changed = false;

        foreach (var item in await _db.ContactMessages.Where(x => x.AttachmentPath != null).ToListAsync(cancellationToken))
            changed |= await TryMigrateAsync(item.AttachmentPath, "contact-messages", value => item.AttachmentPath = value, cancellationToken);

        foreach (var item in await _db.FeedbackEntries.Where(x => x.AttachmentPath != null).ToListAsync(cancellationToken))
            changed |= await TryMigrateAsync(item.AttachmentPath, "feedback-attachments", value => item.AttachmentPath = value, cancellationToken);

        foreach (var item in await _db.JobApplications.Where(x => x.CvFilePath != null || x.AttachmentFilePath != null).ToListAsync(cancellationToken))
        {
            changed |= await TryMigrateAsync(item.CvFilePath, "job-applications-cv", value => item.CvFilePath = value, cancellationToken);
            changed |= await TryMigrateAsync(item.AttachmentFilePath, "job-applications-attachments", value => item.AttachmentFilePath = value, cancellationToken);
        }

        foreach (var item in await _db.OpportunityCandidates.Where(x => x.CvFilePath != null).ToListAsync(cancellationToken))
            changed |= await TryMigrateAsync(item.CvFilePath, "organization-candidate-cv", value => item.CvFilePath = value, cancellationToken);

        foreach (var item in await _db.DonorReports.Where(x => x.FilePath != null).ToListAsync(cancellationToken))
            changed |= await TryMigrateAsync(item.FilePath, "donor-reports", value => item.FilePath = value, cancellationToken);

        if (changed)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Legacy private upload paths were migrated to protected storage.");
        }
    }

    private async Task<bool> TryMigrateAsync(
        string? oldPath,
        string targetFolder,
        Action<string> assign,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(oldPath) ||
            oldPath.StartsWith("/secure-files/", StringComparison.OrdinalIgnoreCase) ||
            !oldPath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (_migrated.TryGetValue(oldPath, out var cached))
        {
            assign(cached);
            return true;
        }

        var relative = oldPath["/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar);
        var uploadsRoot = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "uploads"));
        var sourcePath = Path.GetFullPath(Path.Combine(uploadsRoot, relative));
        var normalizedRoot = uploadsRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!sourcePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(sourcePath))
        {
            _logger.LogWarning("Legacy private file path could not be migrated because the file was not found: {Path}", oldPath);
            return false;
        }

        try
        {
            await using var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileName = Path.GetFileName(sourcePath);
            var formFile = new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = GetContentType(Path.GetExtension(fileName))
            };

            var newPath = await _files.UploadAsync(formFile, targetFolder, cancellationToken);
            assign(newPath);
            _migrated[oldPath] = newPath;

            try
            {
                File.Delete(sourcePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Migrated legacy file but could not remove original file {Path}", oldPath);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate legacy private file {Path}", oldPath);
            return false;
        }
    }

    private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        _ => "application/octet-stream"
    };
}
