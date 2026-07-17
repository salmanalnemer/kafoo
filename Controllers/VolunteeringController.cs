using Microsoft.AspNetCore.Mvc;

namespace Kafo.Web.Controllers;

public class VolunteeringController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
