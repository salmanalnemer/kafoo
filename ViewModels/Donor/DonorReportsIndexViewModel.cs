using Kafo.Web.Models.Donors;

namespace Kafo.Web.ViewModels.Donor;

public class DonorReportsIndexViewModel
{
    public List<DonorContributionReportItemViewModel> Contributions { get; set; } = new();
    public int TotalContributions { get; set; }
    public decimal TotalSupportAmount { get; set; }
    public decimal TotalSpentAmount { get; set; }
    public int TotalBeneficiaries { get; set; }
    public int DownloadableFiles { get; set; }
    public int? SelectedYear { get; set; }
    public string? SelectedStatus { get; set; }
    public List<int> AvailableYears { get; set; } = new();
}

public class DonorContributionReportItemViewModel
{
    public DonorContribution Contribution { get; set; } = new();
    public List<DonorReport> Reports { get; set; } = new();
}

public class DonorSupportPrintViewModel
{
    public string DonorName { get; set; } = string.Empty;
    public string? DonorCode { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public string ReportTitle { get; set; } = "تقرير الدعم";
    public List<DonorContribution> Contributions { get; set; } = new();
    public decimal TotalAmount => Contributions.Sum(x => x.TotalAmount);
    public decimal TotalSpent => Contributions.Sum(x => x.SpentAmount);
    public decimal TotalRemaining => Contributions.Sum(x => x.RemainingAmount);
    public int TotalBeneficiaries => Contributions.Sum(x => x.BeneficiariesCount);
}
