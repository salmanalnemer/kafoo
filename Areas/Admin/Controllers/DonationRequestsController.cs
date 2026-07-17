using Microsoft.AspNetCore.Mvc;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class DonationRequestsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
