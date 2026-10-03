using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EventsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventResponseDto>>> GetEvents()
    {
        return await _context.Events
            .Include(e => e.Category)
            .Select(e => new EventResponseDto
            {
                EventId = e.EventId,
                EventName = e.EventName,
                EventDate = e.EventDate,
                Location = e.Location,
                CategoryName = e.Category.CategoryName
            })
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<EventResponseDto>> CreateEvent([FromBody] CreateEventDto dto)
    {
        var raceEvent = new Event
        {
            EventName = dto.EventName,
            EventDate = dto.EventDate,
            Location = dto.Location,
            CategoryId = dto.CategoryId
        };

        _context.Events.Add(raceEvent);
        await _context.SaveChangesAsync();

        var category = await _context.Categories.FindAsync(dto.CategoryId);

        var response = new EventResponseDto
        {
            EventId = raceEvent.EventId,
            EventName = raceEvent.EventName,
            EventDate = raceEvent.EventDate,
            Location = raceEvent.Location,
            CategoryName = category?.CategoryName ?? "Unassigned"
        };

        return CreatedAtAction(nameof(GetEvents), new { id = raceEvent.EventId }, response);
    }
}