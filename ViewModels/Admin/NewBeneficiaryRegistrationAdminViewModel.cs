using Kafo.Web.Models;

namespace Kafo.Web.ViewModels.Admin;

public class NewBeneficiaryRegistrationAdminViewModel
{
    public NewBeneficiaryRegistrationPage Page { get; set; } = new();

    public IReadOnlyList<NewBeneficiaryRegistrationRequirement> Requirements { get; set; } =
        new List<NewBeneficiaryRegistrationRequirement>();

    public NewBeneficiaryRegistrationRequirement NewRequirement { get; set; } = new()
    {
        Icon = "▣",
        IsActive = true
    };
}
