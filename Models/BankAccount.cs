using System.ComponentModel.DataAnnotations;

namespace Kafo.Web.Models;

public class BankAccount
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم البنك مطلوب")]
    [MaxLength(180)]
    public string BankName { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم الحساب مطلوب")]
    [MaxLength(220)]
    public string AccountName { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم الآيبان مطلوب")]
    [MaxLength(80)]
    public string Iban { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? AccountNumber { get; set; }

    [MaxLength(500)]
    public string? LogoPath { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
