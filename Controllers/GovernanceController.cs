using Microsoft.AspNetCore.Mvc;

namespace Kafo.Web.Controllers;

public class GovernanceController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
