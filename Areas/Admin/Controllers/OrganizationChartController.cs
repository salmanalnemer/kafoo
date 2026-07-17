using Microsoft.AspNetCore.Mvc;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class OrganizationChartController : Controller
{
    public IActionResult Index()
    {
        return Redirect("/Admin/Pages/OrganizationalStructure");
    }
}
