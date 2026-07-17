using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class PartnersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;

    public PartnersController(ApplicationDbContext context, IFileUploadService files)
    {
        _context = context;
        _files = files;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _context.SuccessPartners
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(items);
    }

    public IActionResult Create()
    {
        return View(new SuccessPartner { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SuccessPartner model, IFormFile? logoFile)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (logoFile != null)
            model.LogoPath = await _files.UploadAsync(logoFile, "partners");

        model.CreatedAt = DateTime.Now;
        _context.SuccessPartners.Add(model);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.SuccessPartners.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SuccessPartner model, IFormFile? logoFile)
    {
        if (id != model.Id)
            return BadRequest();

        var item = await _context.SuccessPartners.FindAsync(id);
        if (item == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        if (logoFile != null)
        {
            _files.Delete(item.LogoPath);
            item.LogoPath = await _files.UploadAsync(logoFile, "partners");
        }

        item.Name = model.Name;
        item.WebsiteUrl = model.WebsiteUrl;
        item.DisplayOrder = model.DisplayOrder;
        item.IsActive = model.IsActive;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.SuccessPartners.FindAsync(id);
        if (item == null)
            return NotFound();

        _files.Delete(item.LogoPath);
        _context.SuccessPartners.Remove(item);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
