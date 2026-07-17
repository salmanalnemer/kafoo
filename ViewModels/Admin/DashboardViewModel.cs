namespace Kafo.Web.ViewModels.Admin;

public class DashboardViewModel
{
    public int SlidersCount { get; set; }
    public int ActiveSlidersCount { get; set; }

    public int StatisticsCount { get; set; }
    public int GoalsCount { get; set; }

    public int ProgramsCount { get; set; }
    public int ActiveProgramsCount { get; set; }

    public int NewsCount { get; set; }
    public int ActiveNewsCount { get; set; }

    public int PartnersCount { get; set; }
    public int ActivePartnersCount { get; set; }

    public IReadOnlyList<DashboardItemViewModel> LatestPrograms { get; set; } = [];
    public IReadOnlyList<DashboardItemViewModel> LatestNews { get; set; } = [];
    public IReadOnlyList<DashboardItemViewModel> LatestSliders { get; set; } = [];
}

public class DashboardItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string EditUrl { get; set; } = "#";
    public string PreviewUrl { get; set; } = "#";
}
