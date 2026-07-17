namespace Kafo.Web.Models;

public class InitiativeGallery
{
    public int Id { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
