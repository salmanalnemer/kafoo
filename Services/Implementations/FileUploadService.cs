using System.Buffers;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Kafo.Web.Configuration;
using Kafo.Web.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Kafo.Web.Services.Implementations;

public sealed partial class FileUploadService : IFileUploadService
{
    private static readonly HashSet<string> PrivateFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "contact-messages",
        "feedback-attachments",
        "job-applications-cv",
        "job-applications-attachments",
        "organization-candidate-cv",
        "donor-reports"
    };

    private static readonly HashSet<string> ImageFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin-profiles", "aid-report-covers", "banks", "board-members",
        "general-assembly-members", "general-assembly-minutes-covers", "initiatives", "news", "pages",
        "executive-manager", "executive-manager-signatures", "partners",
        "president", "programs", "service-links", "sliders", "success-partners",
        "team", "video-thumbnails", "volunteer-opportunities", "organizations",
        "organizational-structure"
    };

    private static readonly HashSet<string> VideoFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "video-library", "beneficiary-update-videos", "new-beneficiary-videos"
    };

    private static readonly HashSet<string> CvFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "job-applications-cv", "organization-candidate-cv"
    };

    private static readonly HashSet<string> DocumentFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "aid-reports", "annual-reports", "committee-documents", "financial-statements",
        "general-assembly-minutes", "licenses", "operational-plans", "policy-documents",
        "quarter-reports"
    };

    private static readonly HashSet<string> AllowedFolders = new(
        PrivateFolders
            .Concat(ImageFolders)
            .Concat(VideoFolders)
            .Concat(DocumentFolders),
        StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string[]> AllowedMimeTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".jfif"] = ["image/jpeg"],
            [".png"] = ["image/png"],
            [".gif"] = ["image/gif"],
            [".webp"] = ["image/webp"],
            [".avif"] = ["image/avif", "image/heif", "image/heic"],
            [".pdf"] = ["application/pdf", "application/octet-stream"],
            [".doc"] = ["application/msword", "application/octet-stream"],
            [".docx"] =
            [
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/zip",
                "application/octet-stream"
            ],
            [".xls"] = ["application/vnd.ms-excel", "application/octet-stream"],
            [".xlsx"] =
            [
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/zip",
                "application/octet-stream"
            ],
            [".mp4"] = ["video/mp4", "application/octet-stream"],
            [".webm"] = ["video/webm", "application/octet-stream"],
            [".mov"] = ["video/quicktime", "application/octet-stream"]
        };

    private readonly IWebHostEnvironment _environment;
    private readonly SecurityOptions _options;
    private readonly ILogger<FileUploadService> _logger;
    private readonly IFileMalwareScanner _malwareScanner;
    private readonly string _privateRoot;
    private readonly string _publicRoot;

    public FileUploadService(
        IWebHostEnvironment environment,
        IOptions<SecurityOptions> options,
        IFileMalwareScanner malwareScanner,
        ILogger<FileUploadService> logger)
    {
        _environment = environment;
        _options = options.Value;
        _malwareScanner = malwareScanner;
        _logger = logger;

        _privateRoot = Path.GetFullPath(Path.IsPathRooted(_options.PrivateStoragePath)
            ? _options.PrivateStoragePath
            : Path.Combine(_environment.ContentRootPath, _options.PrivateStoragePath));

        var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;
        _publicRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads"));
    }

    public async Task<string> UploadAsync(
        IFormFile file,
        string folderName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        cancellationToken.ThrowIfCancellationRequested();

        if (file.Length <= 0)
            throw new InvalidOperationException("لم يتم اختيار ملف صالح.");

        var safeFolder = NormalizeFolder(folderName);
        var originalLeafName = Path.GetFileName(file.FileName ?? string.Empty);
        var extension = Path.GetExtension(originalLeafName).ToLowerInvariant();
        var profile = GetProfile(safeFolder);

        if (string.IsNullOrWhiteSpace(extension) ||
            !profile.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("نوع الملف غير مسموح لهذا الحقل.");
        }

        if (file.Length > profile.MaxBytes)
        {
            throw new InvalidOperationException(
                $"حجم الملف يتجاوز الحد المسموح وهو {profile.MaxBytes / 1024 / 1024} ميجابايت.");
        }

        var suppliedContentType = (file.ContentType ?? string.Empty).Trim();
        if (!AllowedMimeTypes.TryGetValue(extension, out var allowedMimes) ||
            !allowedMimes.Contains(suppliedContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("نوع محتوى الملف لا يطابق الامتداد المسموح.");
        }

        await ValidateSignatureAsync(file, extension, cancellationToken);

        if (extension is ".docx" or ".xlsx")
            await ValidateOpenXmlContainerAsync(file, extension, cancellationToken);

        // الفحص يتم قبل إنشاء الملف الدائم.
        await _malwareScanner.ScanAsync(file, cancellationToken);

        var isPrivate = PrivateFolders.Contains(safeFolder);
        var root = isPrivate ? _privateRoot : _publicRoot;
        var targetDirectory = Path.GetFullPath(Path.Combine(root, safeFolder));
        EnsureInsideRoot(root, targetDirectory);
        Directory.CreateDirectory(targetDirectory);

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var finalPath = Path.GetFullPath(Path.Combine(targetDirectory, storedName));
        var temporaryPath = Path.GetFullPath(Path.Combine(
            targetDirectory,
            $".{Guid.NewGuid():N}.uploading"));

        EnsureInsideRoot(root, finalPath);
        EnsureInsideRoot(root, temporaryPath);

        try
        {
            await using (var source = file.OpenReadStream())
            await using (var target = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             64 * 1024,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var copiedBytes = await CopyWithLimitAsync(
                    source,
                    target,
                    profile.MaxBytes,
                    cancellationToken);

                if (copiedBytes != file.Length)
                    throw new InvalidOperationException("تعذر التحقق من الحجم الحقيقي للملف.");

                await target.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, finalPath, overwrite: false);
        }
        catch
        {
            SafeDeletePhysicalFile(temporaryPath);
            SafeDeletePhysicalFile(finalPath);
            throw;
        }

        _logger.LogInformation(
            "Accepted upload {Folder}/{FileName}; extension {Extension}; size {Size}; private {IsPrivate}",
            safeFolder,
            storedName,
            extension,
            file.Length,
            isPrivate);

        return isPrivate
            ? $"/secure-files/{safeFolder}/{storedName}"
            : $"/uploads/{safeFolder}/{storedName}";
    }

    public void Delete(string? filePath)
    {
        if (!TryResolveManagedPath(filePath, out var fullPath))
            return;

        try
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Unable to delete uploaded file {FilePath}", filePath);
        }
    }

    public bool TryResolvePrivateFile(
        string folderName,
        string fileName,
        out PrivateFileDescriptor? descriptor)
    {
        descriptor = null;

        var normalizedFolder = (folderName ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedFile = Path.GetFileName(fileName ?? string.Empty);

        if (!SafeFolderRegex().IsMatch(normalizedFolder) ||
            !SafeStoredFileRegex().IsMatch(normalizedFile) ||
            !PrivateFolders.Contains(normalizedFolder))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(Path.Combine(_privateRoot, normalizedFolder, normalizedFile));
        try
        {
            EnsureInsideRoot(_privateRoot, fullPath);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        if (!File.Exists(fullPath))
            return false;

        var extension = Path.GetExtension(normalizedFile).ToLowerInvariant();
        descriptor = new PrivateFileDescriptor(
            fullPath,
            GetContentType(extension),
            $"file{extension}");
        return true;
    }

    private bool TryResolveManagedPath(string? filePath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var normalized = filePath.Trim().Replace('\\', '/');
        string root;
        string relative;

        if (normalized.StartsWith("/secure-files/", StringComparison.OrdinalIgnoreCase))
        {
            root = _privateRoot;
            relative = normalized["/secure-files/".Length..];
        }
        else if (normalized.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            root = _publicRoot;
            relative = normalized["/uploads/".Length..];
        }
        else
        {
            return false;
        }

        var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return false;

        var folder = parts[0].ToLowerInvariant();
        var storedFile = parts[1];

        if (!AllowedFolders.Contains(folder) ||
            !SafeFolderRegex().IsMatch(folder) ||
            !SafeStoredFileRegex().IsMatch(storedFile))
        {
            return false;
        }

        if (root == _privateRoot && !PrivateFolders.Contains(folder))
            return false;

        if (root == _publicRoot && PrivateFolders.Contains(folder))
            return false;

        try
        {
            fullPath = Path.GetFullPath(Path.Combine(root, folder, storedFile));
            EnsureInsideRoot(root, fullPath);
            return true;
        }
        catch (InvalidOperationException)
        {
            fullPath = string.Empty;
            return false;
        }
    }

    private static UploadProfile GetProfile(string folder)
    {
        if (ImageFolders.Contains(folder))
        {
            return new UploadProfile(
                [".jpg", ".jpeg", ".jfif", ".png", ".gif", ".webp", ".avif"],
                5 * 1024 * 1024);
        }

        if (VideoFolders.Contains(folder))
            return new UploadProfile([".mp4", ".webm", ".mov"], 100 * 1024 * 1024);

        if (CvFolders.Contains(folder))
            return new UploadProfile([".pdf", ".doc", ".docx"], 10 * 1024 * 1024);

        if (string.Equals(folder, "donor-reports", StringComparison.OrdinalIgnoreCase))
        {
            return new UploadProfile(
                [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png"],
                20 * 1024 * 1024);
        }

        if (PrivateFolders.Contains(folder))
        {
            return new UploadProfile(
                [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"],
                10 * 1024 * 1024);
        }

        if (DocumentFolders.Contains(folder))
            return new UploadProfile([".pdf", ".doc", ".docx", ".xls", ".xlsx"], 20 * 1024 * 1024);

        throw new InvalidOperationException("تصنيف رفع الملف غير معتمد.");
    }

    private static async Task ValidateSignatureAsync(
        IFormFile file,
        string extension,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(32);
        try
        {
            await using var stream = file.OpenReadStream();
            var read = await stream.ReadAsync(buffer.AsMemory(0, 32), cancellationToken);
            if (!SignatureMatches(buffer.AsSpan(0, read), extension))
                throw new InvalidOperationException("محتوى الملف لا يطابق امتداده أو أن الملف تالف.");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    private static async Task ValidateOpenXmlContainerAsync(
        IFormFile file,
        string extension,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = file.OpenReadStream();

        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            if (archive.Entries.Count is 0 or > 2048)
                throw new InvalidOperationException("بنية ملف Office غير صالحة.");

            var requiredEntry = extension == ".docx" ? "word/document.xml" : "xl/workbook.xml";
            var hasContentTypes = false;
            var hasRequiredEntry = false;
            long totalUncompressedBytes = 0;

            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entryName = entry.FullName.Replace('\\', '/');
                if (entryName.StartsWith("/", StringComparison.Ordinal) ||
                    entryName.Contains("../", StringComparison.Ordinal) ||
                    entryName.Contains("/..", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("بنية ملف Office تحتوي على مسارات غير آمنة.");
                }

                if (entryName.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase) ||
                    entryName.Contains("/embeddings/", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("ملفات Office التي تحتوي على ماكرو أو كائنات مضمّنة غير مسموحة.");
                }

                totalUncompressedBytes += entry.Length;
                if (totalUncompressedBytes > 100L * 1024 * 1024)
                    throw new InvalidOperationException("حجم محتويات ملف Office بعد فك الضغط غير آمن.");

                hasContentTypes |= string.Equals(
                    entryName,
                    "[Content_Types].xml",
                    StringComparison.OrdinalIgnoreCase);
                hasRequiredEntry |= string.Equals(
                    entryName,
                    requiredEntry,
                    StringComparison.OrdinalIgnoreCase);
            }

            if (!hasContentTypes || !hasRequiredEntry)
                throw new InvalidOperationException("امتداد ملف Office لا يطابق محتواه الفعلي.");
        }
        catch (InvalidDataException)
        {
            throw new InvalidOperationException("ملف Office تالف أو غير صالح.");
        }
    }

    private static async Task<long> CopyWithLimitAsync(
        Stream source,
        Stream destination,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        long total = 0;

        try
        {
            while (true)
            {
                var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                    break;

                total += read;
                if (total > maximumBytes)
                    throw new InvalidOperationException("حجم الملف الحقيقي يتجاوز الحد المسموح.");

                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            return total;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    private static bool SignatureMatches(ReadOnlySpan<byte> bytes, string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" or ".jfif" => StartsWith(bytes, 0xFF, 0xD8, 0xFF),
            ".png" => StartsWith(bytes, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
            ".gif" => HasAscii(bytes, 0, "GIF87a") || HasAscii(bytes, 0, "GIF89a"),
            ".webp" => HasAscii(bytes, 0, "RIFF") && HasAscii(bytes, 8, "WEBP"),
            ".avif" => HasAscii(bytes, 4, "ftyp") &&
                       (HasAscii(bytes, 8, "avif") || HasAscii(bytes, 8, "avis")),
            ".pdf" => HasAscii(bytes, 0, "%PDF-"),
            ".doc" or ".xls" => StartsWith(bytes, 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1),
            ".docx" or ".xlsx" => StartsWith(bytes, 0x50, 0x4B, 0x03, 0x04) ||
                                     StartsWith(bytes, 0x50, 0x4B, 0x05, 0x06),
            ".mp4" or ".mov" => HasAscii(bytes, 4, "ftyp"),
            ".webm" => StartsWith(bytes, 0x1A, 0x45, 0xDF, 0xA3),
            _ => false
        };
    }

    private static bool StartsWith(ReadOnlySpan<byte> bytes, params byte[] signature)
        => bytes.Length >= signature.Length && bytes[..signature.Length].SequenceEqual(signature);

    private static bool HasAscii(ReadOnlySpan<byte> bytes, int offset, string text)
    {
        if (bytes.Length < offset + text.Length)
            return false;

        for (var index = 0; index < text.Length; index++)
        {
            if (bytes[offset + index] != (byte)text[index])
                return false;
        }

        return true;
    }

    private static string NormalizeFolder(string folderName)
    {
        var value = string.IsNullOrWhiteSpace(folderName)
            ? string.Empty
            : folderName.Trim().ToLowerInvariant();

        if (!SafeFolderRegex().IsMatch(value) || !AllowedFolders.Contains(value))
            throw new InvalidOperationException("اسم مجلد الرفع غير صالح أو غير معتمد.");

        return value;
    }

    private static void EnsureInsideRoot(string root, string candidate)
    {
        var normalizedRoot = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        var normalizedCandidate = Path.GetFullPath(candidate);

        if (!normalizedCandidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("مسار الملف غير صالح.");
    }

    private static void SafeDeletePhysicalFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // تنظيف best-effort، والخطأ الأصلي هو الذي يجب أن يصل للمستدعي.
        }
    }

    private static string GetContentType(string extension) => extension switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".jpg" or ".jpeg" or ".jfif" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".avif" => "image/avif",
        ".mp4" => "video/mp4",
        ".webm" => "video/webm",
        ".mov" => "video/quicktime",
        _ => "application/octet-stream"
    };

    private sealed record UploadProfile(string[] Extensions, long MaxBytes);

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeFolderRegex();

    [GeneratedRegex(
        "^[a-f0-9]{32}\\.(pdf|doc|docx|xls|xlsx|jpg|jpeg|jfif|png|gif|webp|avif|mp4|webm|mov)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex SafeStoredFileRegex();
}
