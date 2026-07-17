using Microsoft.AspNetCore.Mvc;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class JobsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
