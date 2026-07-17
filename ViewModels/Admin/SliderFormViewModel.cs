using System.ComponentModel.DataAnnotations;
using Kafo.Web.Security;
using Microsoft.AspNetCore.Http;

namespace Kafo.Web.ViewModels.Admin;

public class SliderFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "عنوان السلايدر")]
    public string? Title { get; set; }

    [Display(Name = "وصف السلايدر")]
    public string? Description { get; set; }

    [Display(Name = "نص الزر")]
    public string? ButtonText { get; set; }

    [Display(Name = "رابط الزر")]
    [SafeNavigationUrl]
    public string? ButtonUrl { get; set; }

    [Display(Name = "ترتيب الظهور")]
    public int DisplayOrder { get; set; } = 0;

    [Display(Name = "مفعل")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "تاريخ بداية النشر")]
    public DateTime? PublishStart { get; set; }

    [Display(Name = "تاريخ نهاية النشر")]
    public DateTime? PublishEnd { get; set; }

    public string? CurrentImagePath { get; set; }

    [Display(Name = "صورة السلايدر")]
    public IFormFile? ImageFile { get; set; }
}
