using Kafo.Web.Data;
using Kafo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

public class PresidentController : Controller
{
    private readonly ApplicationDbContext _context;

    public PresidentController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var page = await _context.PresidentMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive);

        if (page == null)
        {
            page = new PresidentMessage
            {
                LeaderName = "اسم رئيس الجمعية",
                PositionTitle = "رئيس مجلس الإدارة",
                Title = "كلمة رئيس الجمعية",
                MessageText = "نرحب بكم في جمعية كفؤ لتمكين ذوي الإعاقة بحائل، ونسعى من خلال برامجنا ومبادراتنا إلى تحسين جودة حياة ذوي الإعاقة وتمكينهم من المشاركة الفاعلة في المجتمع وسوق العمل، بما يتوافق مع مستهدفات رؤية المملكة 2030."
            };
        }

        return View(page);
    }
}
