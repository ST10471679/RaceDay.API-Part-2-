namespace RaceDay.API.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int UserId { get; set; }
    public int EventId { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Event Event { get; set; } = null!;
}