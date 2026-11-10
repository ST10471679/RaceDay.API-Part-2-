using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResultsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ResultsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Results already captured for an event (used to pre-fill the organiser's form)
    [HttpGet("event/{eventId:int}")]
    public async Task<ActionResult<IEnumerable<ResultEntryDto>>> GetEventResults(int eventId)
    {
        var results = await _context.Results
            .AsNoTracking()
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.Position)
            .ToListAsync();

        return Ok(results.Select(r => new ResultEntryDto
        {
            UserId = r.UserId,
            FinishTime = ResultTimeFormatter.Format(r.CompletionTime),
            Position = r.Position
        }));
    }

    // Save (replace) all results for an event
    [HttpPost("event/{eventId:int}")]
    public async Task<IActionResult> SaveEventResults(int eventId, [FromBody] SaveResultsDto dto)
    {
        var raceEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (raceEvent == null) return NotFound(new { message = "Event not found." });

        if (raceEvent.OrganiserId.HasValue && raceEvent.OrganiserId != dto.OrganiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only capture results for events that you created." });
        }

        var enrolledList = await _context.Enrolments
            .Where(e => e.EventId == eventId)
            .Include(e => e.User)
            .ToListAsync();

        var enrolledUsers = enrolledList
            .GroupBy(e => e.UserId)
            .ToDictionary(g => g.Key, g => $"{g.First().User.FirstName} {g.First().User.LastName}".Trim());

        // Rows without a finish time are simply "no result yet"
        var rows = new List<(int UserId, TimeSpan Time, int? Position)>();
        foreach (var entry in dto.Results.Where(r => !string.IsNullOrWhiteSpace(r.FinishTime)))
        {
            if (!enrolledUsers.TryGetValue(entry.UserId, out var name))
            {
                return BadRequest(new { message = "A result was supplied for someone who is not enrolled in this event." });
            }

            if (!ResultTimeFormatter.TryParse(entry.FinishTime, out var time))
            {
                return BadRequest(new { message = $"\"{entry.FinishTime}\" is not a valid finish time for {name}. Use hh:mm:ss, e.g. 00:45:30." });
            }

            if (entry.Position is <= 0)
            {
                return BadRequest(new { message = $"The position for {name} must be 1 or higher." });
            }

            rows.Add((entry.UserId, time, entry.Position));
        }

        if (rows.GroupBy(r => r.UserId).Any(g => g.Count() > 1))
        {
            return BadRequest(new { message = "Each participant can only have one result." });
        }

        // If every row has a position use them (must be unique); otherwise rank everyone by time
        List<(int UserId, TimeSpan Time, int Position)> final;
        if (rows.Count > 0 && rows.All(r => r.Position.HasValue))
        {
            if (rows.Select(r => r.Position!.Value).Distinct().Count() != rows.Count)
            {
                return BadRequest(new { message = "Each finisher needs a unique position." });
            }
            final = rows.Select(r => (r.UserId, r.Time, r.Position!.Value)).ToList();
        }
        else
        {
            final = rows.OrderBy(r => r.Time)
                .Select((r, index) => (r.UserId, r.Time, index + 1))
                .ToList();
        }

        _context.Results.RemoveRange(await _context.Results.Where(r => r.EventId == eventId).ToListAsync());

        foreach (var row in final)
        {
            _context.Results.Add(new Result
            {
                EventId = eventId,
                UserId = row.UserId,
                CompletionTime = row.Time,
                Position = row.Position
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Results saved for {final.Count} finisher(s).", finishers = final.Count });
    }

    // A participant's own results, with their place out of all finishers
    [HttpGet("my-results")]
    public async Task<ActionResult<IEnumerable<MyResultDto>>> GetMyResults([FromQuery] int userId)
    {
        var mine = await _context.Results
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .Include(r => r.Event)
            .OrderByDescending(r => r.Event.EventDate)
            .ToListAsync();

        var eventIds = mine.Select(r => r.EventId).Distinct().ToList();

        var totals = await _context.Results
            .Where(r => eventIds.Contains(r.EventId))
            .GroupBy(r => r.EventId)
            .Select(g => new { EventId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Total);

        var categories = await _context.Enrolments
            .AsNoTracking()
            .Where(e => e.UserId == userId && eventIds.Contains(e.EventId))
            .Include(e => e.Category)
            .ToListAsync();

        var response = mine.Select(r => new MyResultDto
        {
            EventId = r.EventId,
            EventName = r.Event.EventName,
            EventDate = r.Event.EventDate,
            CategoryName = categories.FirstOrDefault(c => c.EventId == r.EventId)?.Category?.Name ?? "Unassigned",
            FinishTime = ResultTimeFormatter.Format(r.CompletionTime),
            Position = r.Position,
            TotalFinishers = totals.TryGetValue(r.EventId, out var total) ? total : 0
        }).ToList();

        return Ok(response);
    }
}
