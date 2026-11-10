namespace RaceDay.API.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int UserId { get; set; }
    public int EventId { get; set; }

    // The category the participant chose for this event
    public int? CategoryId { get; set; }

    // "Confirmed" or "Pending" (waiting list when the category is full)
    public string Status { get; set; } = "Confirmed";
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public EventCategory? Category { get; set; }
}
