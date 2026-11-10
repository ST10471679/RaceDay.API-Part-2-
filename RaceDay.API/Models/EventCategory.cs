namespace RaceDay.API.Models;

/// <summary>
/// A race category that belongs to ONE event (e.g. "21km", "10km Fun Run").
/// An event can have many of these. Participants choose one when they enrol.
/// </summary>
public class EventCategory
{
    public int EventCategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Distance { get; set; } = string.Empty;
    public decimal EntryFee { get; set; }

    // 0 = unlimited. When the limit is reached new enrolments are marked "Pending" (waiting list).
    public int MaxParticipants { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
