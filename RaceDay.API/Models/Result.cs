namespace RaceDay.API.Models;

public class Result
{
    public int ResultId { get; set; }
    public int UserId { get; set; }
    public int EventId { get; set; }
    public TimeSpan CompletionTime { get; set; }
    public int Position { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Event Event { get; set; } = null!;
}