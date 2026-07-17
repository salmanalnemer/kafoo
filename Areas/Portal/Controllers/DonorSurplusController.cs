using System.Security.Claims;
using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Portal.Controllers;

[Area("Portal")]
public class DonorSurplusController : Controller
{
    private readonly ApplicationDbContext _context;

    public DonorSurplusController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Portal/Donor/Surplus")]
    public async Task<IActionResult> Index()
    {
        var donorId = GetDonorId();

        var items = await _context.DonorContributions
            .AsNoTracking()
            .Include(x => x.ProgramProject)
            .Include(x => x.SurplusDecisions.OrderByDescending(d => d.ApprovedAt))
            .Where(x => x.DonorAccountId == donorId && (x.HasSurplus || x.SurplusDecisions.Any()))
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

        return View("~/Areas/Portal/Views/DonorSurplus/Index.cshtml", items);
    }

    private int GetDonorId()
    {
        var value = User.FindFirstValue("KafoDonorUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var donorId) ? donorId : 0;
    }
}
