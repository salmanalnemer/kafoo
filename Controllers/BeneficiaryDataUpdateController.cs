using Kafo.Web.Data;
using Kafo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

public class BeneficiaryDataUpdateController : Controller
{
    private readonly ApplicationDbContext _context;

    public BeneficiaryDataUpdateController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var page = await _context.BeneficiaryDataUpdatePages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive);

        if (page == null)
        {
            page = new BeneficiaryDataUpdatePage();
        }

        var requirements = await _context.BeneficiaryDataUpdateRequirements
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        if (!requirements.Any())
        {
            requirements =
            [
                new BeneficiaryDataUpdateRequirement { Title = "صورة الهوية الوطنية", Icon = "▣", DisplayOrder = 1 },
                new BeneficiaryDataUpdateRequirement { Title = "مشهد الإعاقة أو التقرير الطبي", Icon = "▣", DisplayOrder = 2 },
                new BeneficiaryDataUpdateRequirement { Title = "رقم الجوال والبريد الإلكتروني", Icon = "▣", DisplayOrder = 3 },
                new BeneficiaryDataUpdateRequirement { Title = "العنوان الوطني", Icon = "▣", DisplayOrder = 4 }
            ];
        }

        ViewBag.Requirements = requirements;

        return View(page);
    }
}
