using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[Route("BoardMembers")]
[Route("Governance/BoardMembers")]
public class BoardMembersController : Controller
{
    private readonly ApplicationDbContext _context;

    public BoardMembersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var members = await _context.BoardMembers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsChairman)
            .ThenByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.FullName)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        return View("~/Views/BoardMembers/Index.cshtml", members);
    }
}
