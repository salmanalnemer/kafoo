using Kafo.Web.Data;
using Kafo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

public class AboutController : Controller
{
    private readonly ApplicationDbContext _context;

    public AboutController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var page = await _context.SiteContentPages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PageKey == "about" && x.IsActive);

        if (page == null)
        {
            page = new SiteContentPage
            {
                PageKey = "about",
                Title = "من نحن",
                Subtitle = "جمعية كفؤ لتمكين ذوي الإعاقة بحائل",
                Content = "جمعية كفؤ لتمكين ذوي الإعاقة بحائل تسعى لتحسين جودة حياة ذوي الإعاقة عبر التدريب والتأهيل المهني، وزيادة وعي المجتمع، وتمكينهم من المشاركة الفاعلة في المجتمع وسوق العمل بما يتناسب مع قدراتهم وإمكاناتهم، انسجامًا مع رؤية المملكة 2030."
            };
        }

        return View(page);
    }
}
