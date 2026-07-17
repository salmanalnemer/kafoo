using System.Security.Claims;
using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Kafo.Web.Areas.Portal.Controllers;
[Area("Portal")]
public class OrganizationCandidatesController : Controller
{
    private readonly ApplicationDbContext _context;
    public OrganizationCandidatesController(ApplicationDbContext context)=>_context=context;
    [HttpGet("/Portal/Organization/Candidates")]
    public async Task<IActionResult> Index(string? status)
    {
        var id=GetOrganizationId();
        var query=_context.OpportunityCandidates.AsNoTracking().Include(x=>x.OpportunityRequest)
            .Where(x=>x.OpportunityRequest!=null&&x.OpportunityRequest.OrganizationAccountId==id);
        if(!string.IsNullOrWhiteSpace(status)) query=query.Where(x=>x.Status==status);
        ViewBag.Status=status;
        return View("~/Areas/Portal/Views/OrganizationCandidates/Index.cshtml",await query.OrderByDescending(x=>x.CreatedAt).ToListAsync());
    }
    private int GetOrganizationId()=>int.TryParse(User.FindFirstValue("KafoOrganizationUserId"),out var id)?id:0;
}
