using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[Route("GeneralAssemblyMembers")]
[Route("Governance/GeneralAssemblyMembers")]
public class GeneralAssemblyMembersController : Controller
{
    private readonly ApplicationDbContext _context;

    public GeneralAssemblyMembersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var members = await _context.GeneralAssemblyMembers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.FullName)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        return View("~/Views/GeneralAssemblyMembers/Index.cshtml", members);
    }
}
