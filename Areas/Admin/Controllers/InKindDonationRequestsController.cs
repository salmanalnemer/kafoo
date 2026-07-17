using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/InKindDonationRequests")]
public class InKindDonationRequestsController : Controller
{
    private readonly ApplicationDbContext _context;

    public InKindDonationRequestsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var requests = await _context.InKindDonationRequests
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        return View("~/Areas/Admin/Views/InKindDonationRequests/Index.cshtml", requests);
    }

    [HttpPost("UpdateStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var request = await _context.InKindDonationRequests.FindAsync(id);

        if (request == null)
            return NotFound();

        request.Status = string.IsNullOrWhiteSpace(status) ? "جديد" : status.Trim();

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
