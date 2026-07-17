using Microsoft.AspNetCore.Mvc;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class AdminAccessController : Controller
{
    [HttpGet("/Admin/AccessDenied")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult AccessDenied(string? path = null)
    {
        ViewBag.RequestedPath = path;
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("~/Areas/Admin/Views/Auth/AccessDenied.cshtml");
    }
}
