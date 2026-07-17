using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[Route("Policies")]
[Route("Governance/Policies")]
public class PoliciesController : Controller
{
    private readonly ApplicationDbContext _context;

    public PoliciesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var documents = await _context.PolicyDocuments
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync();

        return View("~/Views/Policies/Index.cshtml", documents);
    }
}
