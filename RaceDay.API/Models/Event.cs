namespace RaceDay.API.Models;

public class Event
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Distance { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }

    // Legacy single-category link. Optional now: real categories live in EventCategories.
    public int? CategoryId { get; set; }
    public int? OrganiserId { get; set; }

    // Navigation properties
    public Category? Category { get; set; }
    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    public ICollection<Result> Results { get; set; } = new List<Result>();
}
