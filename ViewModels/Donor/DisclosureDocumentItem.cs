namespace Kafo.Web.ViewModels.Donor;

public class DisclosureDocumentItem
{
    public DisclosureDocumentItem(string title, string? label, string? description, string? filePath)
    {
        Title = title;
        Label = label;
        Description = description;
        FilePath = filePath;
    }

    public string Title { get; set; }
    public string? Label { get; set; }
    public string? Description { get; set; }
    public string? FilePath { get; set; }
}
