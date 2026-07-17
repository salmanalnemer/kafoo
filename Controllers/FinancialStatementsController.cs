using Kafo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

[Route("FinancialStatements")]
[Route("Governance/FinancialStatements")]
public class FinancialStatementsController : Controller
{
    private readonly ApplicationDbContext _context;

    public FinancialStatementsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var documents = await _context.FinancialStatementDocuments
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync();

        return View("~/Views/FinancialStatements/Index.cshtml", documents);
    }
}
