using Kafo.Web.Models;

namespace Kafo.Web.ViewModels.Admin;

public class VisionMissionAdminViewModel
{
    public VisionMissionCard Form { get; set; } = new();

    public IReadOnlyList<VisionMissionCard> Cards { get; set; } = [];
}
