using Kafo.Web.Models;

namespace Kafo.Web.ViewModels.Admin;

public class BeneficiaryDataUpdateAdminViewModel
{
    public BeneficiaryDataUpdatePage Page { get; set; } = new();

    public IReadOnlyList<BeneficiaryDataUpdateRequirement> Requirements { get; set; } = [];

    public BeneficiaryDataUpdateRequirement NewRequirement { get; set; } = new()
    {
        Icon = "✓",
        IsActive = true
    };
}
