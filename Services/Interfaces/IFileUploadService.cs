namespace Kafo.Web.Services.Interfaces;

public sealed record PrivateFileDescriptor(string FullPath, string ContentType, string DownloadName);

public interface IFileUploadService
{
    Task<string> UploadAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default);
    void Delete(string? filePath);
    bool TryResolvePrivateFile(string folderName, string fileName, out PrivateFileDescriptor? descriptor);
}
