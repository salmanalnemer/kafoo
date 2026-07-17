namespace Kafo.Web.Security;

public static class KafoAuthSchemes
{
    public const string Admin = "KafoAdminScheme";
    public const string Portal = "KafoPortalScheme";

    // kept for backward compatibility with old Donor/Organizations controllers
    public const string Donor = "KafoDonorScheme";
    public const string Organization = "KafoOrganizationScheme";
}
