using Microsoft.AspNetCore.Mvc;

namespace Kafo.Web.Areas.Donor.Controllers;

[Area("Donor")]
public class DonorAuthController : Controller
{
    [AcceptVerbs("GET", "POST")]
    [Route("/Donor/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        var target = string.IsNullOrWhiteSpace(returnUrl)
            ? "/Portal/Login"
            : $"/Portal/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";

        return Redirect(target);
    }

    [AcceptVerbs("GET", "POST")]
    [Route("/Donor/Logout")]
    public IActionResult Logout() => Redirect("/Portal/Logout");
}
