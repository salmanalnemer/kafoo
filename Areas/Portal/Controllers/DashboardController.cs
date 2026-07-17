using Microsoft.AspNetCore.Mvc;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
public class DashboardController : Controller
{
    [HttpGet("/Portal")]
    [HttpGet("/Portal/Dashboard")]
    public IActionResult Index()
    {
        var portalType = User.FindFirst("KafoPortalType")?.Value;

        if (string.Equals(portalType, "Donor", StringComparison.OrdinalIgnoreCase))
            return Redirect("/Portal/Donor/Dashboard");

        if (string.Equals(portalType, "Organization", StringComparison.OrdinalIgnoreCase))
            return Redirect("/Portal/Organization/Dashboard");

        return Redirect("/Portal/Login");
    }
}
