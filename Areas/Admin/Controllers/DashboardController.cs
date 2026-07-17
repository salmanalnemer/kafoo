using Kafo.Web.Data;
using Kafo.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel
        {
            SlidersCount = await _context.Sliders.CountAsync(),
            ActiveSlidersCount = await _context.Sliders.CountAsync(x => x.IsActive),

            StatisticsCount = await _context.HomeStatistics.CountAsync(),
            GoalsCount = await _context.StrategicGoals.CountAsync(),

            ProgramsCount = await _context.ProgramProjects.CountAsync(),
            ActiveProgramsCount = await _context.ProgramProjects.CountAsync(x => x.IsActive),

            NewsCount = await _context.NewsPosts.CountAsync(),
            ActiveNewsCount = await _context.NewsPosts.CountAsync(x => x.IsActive),

            PartnersCount = await _context.SuccessPartners.CountAsync(),
            ActivePartnersCount = await _context.SuccessPartners.CountAsync(x => x.IsActive),

            LatestPrograms = await _context.ProgramProjects
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new DashboardItemViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    ImagePath = x.ImagePath,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    EditUrl = "/Admin/Programs/Edit/" + x.Id,
                    PreviewUrl = "/Programs/Details/" + x.Id
                })
                .ToListAsync(),

            LatestNews = await _context.NewsPosts
                .AsNoTracking()
                .OrderByDescending(x => x.PublishDate)
                .Take(5)
                .Select(x => new DashboardItemViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    ImagePath = x.ImagePath,
                    IsActive = x.IsActive,
                    CreatedAt = x.PublishDate,
                    EditUrl = "/Admin/News/Edit/" + x.Id,
                    PreviewUrl = "/News/Details/" + x.Id
                })
                .ToListAsync(),

            LatestSliders = await _context.Sliders
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new DashboardItemViewModel
                {
                    Id = x.Id,
                    Title = string.IsNullOrWhiteSpace(x.Title) ? "سلايدر بدون عنوان" : x.Title,
                    ImagePath = x.ImagePath,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    EditUrl = "/Admin/Sliders/Edit/" + x.Id,
                    PreviewUrl = "/"
                })
                .ToListAsync()
        };

        return View(model);
    }
}
