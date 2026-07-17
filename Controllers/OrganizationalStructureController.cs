using Kafo.Web.Data;
using Kafo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

public class OrganizationalStructureController : Controller
{
    private readonly ApplicationDbContext _context;

    public OrganizationalStructureController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var page = await _context.OrganizationalStructurePages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive);

        if (page == null)
        {
            page = new OrganizationalStructurePage
            {
                Title = "الهيكل التنظيمي",
                Description = "الهيكل التنظيمي المعتمد للجمعية.",
                ImagePath = null
            };
        }

        return View(page);
    }
}
