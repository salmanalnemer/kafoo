using Kafo.Web.Data;
using Kafo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

public class ExecutiveManagerController : Controller
{
    private readonly ApplicationDbContext _context;

    public ExecutiveManagerController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var page = await _context.ExecutiveManagerMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive);

        if (page == null)
        {
            page = new ExecutiveManagerMessage
            {
                ManagerName = "اسم المدير التنفيذي",
                PositionTitle = "المدير التنفيذي",
                Title = "كلمة المدير التنفيذي",
                MessageText = "نرحب بكم في جمعية كفؤ لتمكين ذوي الإعاقة بحائل، ونعمل مع فريق الجمعية على تطوير البرامج والمبادرات وتحسين الخدمات المقدمة للمستفيدين، بما يعزز جودة الحياة ويدعم التمكين والمشاركة الفاعلة في المجتمع."
            };
        }

        return View(page);
    }
}
