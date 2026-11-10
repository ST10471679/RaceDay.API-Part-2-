namespace RaceDay.API.DTOs;

// One row of the organiser's results form
public class ResultEntryDto
{
    public int UserId { get; set; }

    // "hh:mm:ss" (or "mm:ss"). Blank = did not finish / no result yet.
    public string? FinishTime { get; set; }

    // Blank = rank automatically by finish time
    public int? Position { get; set; }
}

public class SaveResultsDto
{
    public int? OrganiserId { get; set; }
    public List<ResultEntryDto> Results { get; set; } = new();
}

// What a participant sees on "My Results"
public class MyResultDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string FinishTime { get; set; } = string.Empty;
    public int Position { get; set; }
    public int TotalFinishers { get; set; }
}
