using System.ComponentModel.DataAnnotations;
using Kafo.Web.Models;

namespace Kafo.Web.ViewModels;

public class InKindDonationRequestFormViewModel
{
    [Required(ErrorMessage = "الاسم الكريم مطلوب")]
    [MaxLength(180)]
    public string DonorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم الجوال مطلوب")]
    [MaxLength(30)]
    public string Mobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "المدينة مطلوبة")]
    [MaxLength(120)]
    public string City { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? District { get; set; }

    public List<int> SelectedDonationTypeIds { get; set; } = new();

    public string PreferredPickupTime { get; set; } = "morning";

    [MaxLength(1200)]
    public string? Notes { get; set; }

    public bool AcceptTerms { get; set; }

    public IReadOnlyList<InKindDonationType> DonationTypes { get; set; } =
        new List<InKindDonationType>();
}
