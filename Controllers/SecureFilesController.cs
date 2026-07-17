using System.Security.Claims;
using Kafo.Web.Data;
using Kafo.Web.Security;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SecureFilesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IFileUploadService _files;
    private readonly ISecurityAuditService _audit;

    public SecureFilesController(
        ApplicationDbContext db,
        IFileUploadService files,
        ISecurityAuditService audit)
    {
        _db = db;
        _files = files;
        _audit = audit;
    }

    [HttpGet("/secure-files/{folder}/{fileName}")]
    public async Task<IActionResult> Download(
        string folder,
        string fileName,
        CancellationToken cancellationToken)
    {
        if (!_files.TryResolvePrivateFile(folder, fileName, out var descriptor) || descriptor is null)
            return NotFound();

        var requestedPath = $"/secure-files/{folder}/{fileName}";
        var adminAuth = await HttpContext.AuthenticateAsync(KafoAuthSchemes.Admin);
        var portalAuth = await HttpContext.AuthenticateAsync(KafoAuthSchemes.Portal);

        var allowed = false;
        string? actorType = null;
        string? actorId = null;

        if (adminAuth.Succeeded && adminAuth.Principal != null)
        {
            actorType = "Admin";
            actorId = adminAuth.Principal.FindFirstValue("KafoAdminUserId");
            if (int.TryParse(actorId, out var adminId))
                allowed = await CanAdminAccessFolderAsync(adminId, folder, cancellationToken);
        }
        if (!allowed && portalAuth.Succeeded && portalAuth.Principal != null)
        {
            var principal = portalAuth.Principal;
            var portalType = principal.FindFirstValue("KafoPortalType");

            if (string.Equals(folder, "donor-reports", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(portalType, "Donor", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(principal.FindFirstValue("KafoDonorUserId"), out var donorId))
            {
                allowed = await _db.DonorReports
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.FilePath == requestedPath &&
                        x.DonorContribution != null &&
                        x.DonorContribution.DonorAccountId == donorId,
                        cancellationToken);
                actorType = "Donor";
                actorId = donorId.ToString();
            }
            else if (string.Equals(folder, "organization-candidate-cv", StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(portalType, "Organization", StringComparison.OrdinalIgnoreCase) &&
                     int.TryParse(principal.FindFirstValue("KafoOrganizationUserId"), out var organizationId))
            {
                allowed = await _db.OpportunityCandidates
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.CvFilePath == requestedPath &&
                        x.OpportunityRequest != null &&
                        x.OpportunityRequest.OrganizationAccountId == organizationId,
                        cancellationToken);
                actorType = "Organization";
                actorId = organizationId.ToString();
            }
        }

        if (!allowed)
        {
            await _audit.WriteAsync(
                HttpContext,
                "SecureFileAccessDenied",
                $"Denied access to private file category {folder}.",
                success: false,
                severity: "Warning",
                actorType,
                actorId,
                cancellationToken);
            return NotFound();
        }

        await _audit.WriteAsync(
            HttpContext,
            "SecureFileDownloaded",
            $"Private file downloaded from category {folder}.",
            success: true,
            actorType: actorType,
            actorId: actorId,
            cancellationToken: cancellationToken);

        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        Response.Headers.Pragma = "no-cache";
        return PhysicalFile(
            descriptor.FullPath,
            descriptor.ContentType,
            descriptor.DownloadName,
            enableRangeProcessing: true);
    }
    private async Task<bool> CanAdminAccessFolderAsync(
        int adminId,
        string folder,
        CancellationToken cancellationToken)
    {
        var admin = await _db.AdminUsers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == adminId && x.IsActive, cancellationToken);
        if (admin == null)
            return false;

        var roleCode = await AdminRolePolicy.ResolveRoleAsync(_db, admin, cancellationToken);
        if (AdminRolePolicy.HasFullPageAccess(roleCode))
            return true;

        var requiredPath = folder.ToLowerInvariant() switch
        {
            "contact-messages" => "/Admin/Messages",
            "feedback-attachments" => "/Admin/Feedback",
            "job-applications-cv" or "job-applications-attachments" => "/Admin/JobApplications",
            "organization-candidate-cv" => "/Admin/Organizations",
            "donor-reports" => "/Admin/Donors",
            _ => null
        };

        return requiredPath != null && await _db.AdminPagePermissions.AsNoTracking()
            .AnyAsync(x =>
                x.AdminUserId == adminId &&
                x.PagePath == requiredPath &&
                x.CanAccess,
                cancellationToken);
    }

}
