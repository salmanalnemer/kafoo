using Kafo.Web.Data;
using Kafo.Web.Models;
using Kafo.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kafo.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class BankAccountsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _files;
    private readonly ILogger<BankAccountsController> _logger;

    public BankAccountsController(
        ApplicationDbContext context,
        IFileUploadService files,
        ILogger<BankAccountsController> logger)
    {
        _context = context;
        _files = files;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var accounts = await _context.BankAccounts
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        return View(accounts);
    }

    public IActionResult Create()
    {
        return View(new BankAccount
        {
            IsActive = true,
            DisplayOrder = 0,
            AccountName = "جمعية كفؤ لتمكين ذوي الإعاقة بحائل"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BankAccount model, IFormFile? logoFile)
    {
        ModelState.Remove(nameof(BankAccount.LogoPath));
        ModelState.Remove(nameof(BankAccount.CreatedAt));
        ModelState.Remove(nameof(BankAccount.UpdatedAt));

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            if (logoFile != null && logoFile.Length > 0)
                model.LogoPath = await _files.UploadAsync(logoFile, "banks");

            model.BankName = model.BankName.Trim();
            model.AccountName = model.AccountName.Trim();
            model.Iban = NormalizeBankText(model.Iban);
            model.AccountNumber = string.IsNullOrWhiteSpace(model.AccountNumber) ? null : NormalizeBankText(model.AccountNumber);
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            _context.BankAccounts.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة الحساب البنكي بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating bank account");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var account = await _context.BankAccounts.FindAsync(id);

        if (account == null)
            return NotFound();

        return View(account);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BankAccount model, IFormFile? logoFile)
    {
        if (id != model.Id)
            return BadRequest();

        ModelState.Remove(nameof(BankAccount.CreatedAt));
        ModelState.Remove(nameof(BankAccount.UpdatedAt));

        if (!ModelState.IsValid)
            return View(model);

        var account = await _context.BankAccounts.FindAsync(id);

        if (account == null)
            return NotFound();

        try
        {
            if (logoFile != null && logoFile.Length > 0)
            {
                _files.Delete(account.LogoPath);
                account.LogoPath = await _files.UploadAsync(logoFile, "banks");
            }

            account.BankName = model.BankName.Trim();
            account.AccountName = model.AccountName.Trim();
            account.Iban = NormalizeBankText(model.Iban);
            account.AccountNumber = string.IsNullOrWhiteSpace(model.AccountNumber) ? null : NormalizeBankText(model.AccountNumber);
            account.DisplayOrder = model.DisplayOrder;
            account.IsActive = model.IsActive;
            account.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تعديل الحساب البنكي بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while editing bank account {BankAccountId}", id);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var account = await _context.BankAccounts.FindAsync(id);

        if (account == null)
            return NotFound();

        account.IsActive = !account.IsActive;
        account.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var account = await _context.BankAccounts.FindAsync(id);

        if (account == null)
            return NotFound();

        _files.Delete(account.LogoPath);
        _context.BankAccounts.Remove(account);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حذف الحساب البنكي بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    private static string NormalizeBankText(string value)
    {
        return value.Trim().Replace(" ", "").ToUpperInvariant();
    }
}
