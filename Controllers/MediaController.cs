using Microsoft.AspNetCore.Mvc;

namespace Kafo.Web.Controllers;

public class MediaController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
