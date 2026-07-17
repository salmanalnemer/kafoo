using Kafo.Web.Data;
using Kafo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

public class VisionController : Controller
{
    private readonly ApplicationDbContext _context;

    public VisionController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var cards = await _context.VisionMissionCards
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        if (!cards.Any())
        {
            cards =
            [
                new VisionMissionCard
                {
                    Title = "رؤيتنا",
                    Content = "أن نكون كيانًا رائدًا يساهم في تمكين الأشخاص من ذوي الإعاقة.",
                    Icon = "fa-solid fa-eye",
                    DisplayOrder = 1,
                    IsActive = true
                },
                new VisionMissionCard
                {
                    Title = "رسالتنا",
                    Content = "تمكين الأشخاص من ذوي الإعاقة وزيادة فاعليتهم في المجتمع من خلال تعليمهم وتدريبهم وتمكينهم والمساهمة في توفير فرص عمل مناسبة لقدراتهم وإمكاناتهم.",
                    Icon = "fa-solid fa-envelope",
                    DisplayOrder = 2,
                    IsActive = true
                },
                new VisionMissionCard
                {
                    Title = "قيمنا",
                    Content = "الشمولية، التكامل، الابتكار، المسؤولية المجتمعية.",
                    Icon = "fa-solid fa-gem",
                    DisplayOrder = 3,
                    IsActive = true
                }
            ];
        }

        return View(cards);
    }
}
