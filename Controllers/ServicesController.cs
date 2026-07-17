using Microsoft.AspNetCore.Mvc;

namespace Kafo.Web.Controllers;

public class ServicesController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
