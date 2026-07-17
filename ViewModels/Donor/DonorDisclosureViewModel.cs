using Kafo.Web.Models;

namespace Kafo.Web.ViewModels.Donor;

public class DonorDisclosureViewModel
{
    public List<AnnualReportDocument> AnnualReports { get; set; } = new();
    public List<QuarterReportDocument> QuarterReports { get; set; } = new();
    public List<FinancialStatementDocument> FinancialStatements { get; set; } = new();
    public List<OperationalPlanDocument> OperationalPlans { get; set; } = new();
    public List<PolicyDocument> Policies { get; set; } = new();
    public List<AidReport> AidReports { get; set; } = new();
}
