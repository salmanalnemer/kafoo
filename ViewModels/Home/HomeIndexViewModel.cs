using Kafo.Web.Models;

namespace Kafo.Web.ViewModels.Home;

public class HomeIndexViewModel
{
    public IReadOnlyList<Slider> Sliders { get; set; } = [];
    public IReadOnlyList<HomeStatistic> Statistics { get; set; } = [];
    public IReadOnlyList<StrategicGoal> StrategicGoals { get; set; } = [];
    public IReadOnlyList<ProgramProject> Programs { get; set; } = [];
    public IReadOnlyList<NewsPost> News { get; set; } = [];
    public IReadOnlyList<SuccessPartner> Partners { get; set; } = [];
}
