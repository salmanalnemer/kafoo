using Kafo.Web.Data;
using Kafo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Controllers;

public class NewBeneficiaryRegistrationController : Controller
{
    private readonly ApplicationDbContext _context;

    public NewBeneficiaryRegistrationController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var page = await _context.NewBeneficiaryRegistrationPages
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        page ??= new NewBeneficiaryRegistrationPage
        {
            Title = "تسجيل مستفيد جديد",
            Subtitle = "خدمة إلكترونية",
            Description = "يمكنك التسجيل كمستفيد جديد من خلال تعبئة البيانات وإرفاق المستندات المطلوبة.",
            VideoSourceType = "Youtube",
            RegisterButtonText = "التسجيل كمستفيد جديد",
            RegisterButtonUrl = "#",
            AlertTitle = "تنبيه مهم",
            AlertText = "يشترط للتسجيل كمستفيد جديد تعبئة البيانات بدقة وإرفاق المستندات المطلوبة كاملة.",
            WhatsAppNumber = "966500000000",
            WhatsAppButtonText = "طلب مساعدة عبر واتساب",
            WhatsAppMessage = "السلام عليكم، أرغب بالتسجيل كمستفيد جديد وأحتاج المساعدة.",
            OpenRegisterInNewTab = true,
            ShowWhatsAppButton = true,
            IsActive = true
        };

        var requirements = await _context.NewBeneficiaryRegistrationRequirements
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        if (!requirements.Any())
        {
            requirements = new List<NewBeneficiaryRegistrationRequirement>
            {
                new() { Title = "الهوية الوطنية", Note = "صورة واضحة سارية المفعول", Icon = "▣", DisplayOrder = 1, IsActive = true },
                new() { Title = "التقرير الطبي", Note = "تقرير يثبت الحالة أو الإعاقة", Icon = "▣", DisplayOrder = 2, IsActive = true },
                new() { Title = "مشهد الحالة الاجتماعية", Note = "عند الحاجة حسب نوع الطلب", Icon = "▣", DisplayOrder = 3, IsActive = true },
                new() { Title = "بطاقة العائلة", Note = "لمن يلزمهم ذلك", Icon = "▣", DisplayOrder = 4, IsActive = true }
            };
        }

        ViewBag.Requirements = requirements;

        return View(page);
    }
}
