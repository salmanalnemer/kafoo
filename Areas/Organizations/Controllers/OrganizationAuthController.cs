using Microsoft.AspNetCore.Mvc;

namespace Kafo.Web.Areas.Organizations.Controllers;

[Area("Organizations")]
public class OrganizationAuthController : Controller
{
    [AcceptVerbs("GET", "POST")]
    [Route("/Organizations/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        var target = string.IsNullOrWhiteSpace(returnUrl)
            ? "/Portal/Login"
            : $"/Portal/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";

        return Redirect(target);
    }

    [AcceptVerbs("GET", "POST")]
    [Route("/Organizations/Logout")]
    public IActionResult Logout() => Redirect("/Portal/Logout");
}
