using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Security;

public static class PortalEmailPolicy
{
    public static bool IsDeliverable(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            !new EmailAddressAttribute().IsValid(email))
            return false;

        var atIndex = email.LastIndexOf('@');
        if (atIndex < 1 || atIndex == email.Length - 1)
            return false;

        var domain = email[(atIndex + 1)..].Trim().ToLowerInvariant();

        return domain.Length > 0 &&
               !domain.EndsWith(".local", StringComparison.OrdinalIgnoreCase) &&
               !domain.EndsWith(".test", StringComparison.OrdinalIgnoreCase) &&
               !domain.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase) &&
               domain is not "example.com" and not "example.org" and not "example.net";
    }
}
