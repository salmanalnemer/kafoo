using System.Security.Claims;
using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Kafo.Web.Areas.Portal.Controllers;
[Area("Portal")]
public class OrganizationReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    public OrganizationReportsController(ApplicationDbContext context)=>_context=context;
    [HttpGet("/Portal/Organization/Reports")]
    public async Task<IActionResult> Index(){var id=GetOrganizationId();var items=await _context.OpportunityRequests.AsNoTracking().Include(x=>x.Candidates).Where(x=>x.OrganizationAccountId==id).OrderByDescending(x=>x.CreatedAt).ToListAsync();return View("~/Areas/Portal/Views/OrganizationReports/Index.cshtml",items);}
    [HttpGet("/Portal/Organization/Reports/Print/{id:int?}")]
    public async Task<IActionResult> Print(int? id){var oid=GetOrganizationId();var q=_context.OpportunityRequests.AsNoTracking().Include(x=>x.Candidates).Where(x=>x.OrganizationAccountId==oid);if(id.HasValue)q=q.Where(x=>x.Id==id);ViewBag.Organization=await _context.OrganizationAccounts.AsNoTracking().FirstAsync(x=>x.Id==oid);return View("~/Areas/Portal/Views/OrganizationReports/Print.cshtml",await q.OrderByDescending(x=>x.CreatedAt).ToListAsync());}
    private int GetOrganizationId()=>int.TryParse(User.FindFirstValue("KafoOrganizationUserId"),out var id)?id:0;
}
