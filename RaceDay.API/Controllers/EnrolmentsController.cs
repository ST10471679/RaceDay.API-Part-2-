using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrolmentsController : ControllerBase
{
    public const string Confirmed = "Confirmed";
    public const string Pending = "Pending";

    private readonly ApplicationDbContext _context;

    public EnrolmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> EnrolUser([FromQuery] int eventId, [FromQuery] int userId, [FromQuery] int? categoryId = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found. Please log in again." });
        }

        var raceEvent = await _context.Events
            .Include(e => e.EventCategories)
            .FirstOrDefaultAsync(e => e.EventId == eventId);
        if (raceEvent == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (!raceEvent.EventCategories.Any())
        {
            return BadRequest(new { message = "This event has no categories yet, so it cannot be entered." });
        }

        if (categoryId == null)
        {
            return BadRequest(new { message = "Please choose a category." });
        }

        var category = raceEvent.EventCategories.FirstOrDefault(c => c.EventCategoryId == categoryId.Value);
        if (category == null)
        {
            return BadRequest(new { message = "That category does not belong to this event." });
        }

        var alreadyEnrolled = await _context.Enrolments
            .AnyAsync(e => e.UserId == userId && e.EventId == eventId);
        if (alreadyEnrolled)
        {
            return BadRequest(new { message = "You are already enrolled in this event." });
        }

        // Full category => waiting list ("Pending") instead of a confirmed place
        var status = Confirmed;
        if (category.MaxParticipants > 0)
        {
            var taken = await _context.Enrolments.CountAsync(e => e.CategoryId == category.EventCategoryId);
            if (taken >= category.MaxParticipants) status = Pending;
        }

        var enrolment = new Enrolment
        {
            UserId = userId,
            EventId = eventId,
            CategoryId = category.EventCategoryId,
            Status = status,
            EnrolmentDate = DateTime.UtcNow
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        var message = status == Confirmed
            ? "Successfully enrolled in event."
            : "This category is full, so you have been placed on the waiting list.";

        return Ok(new { message, status, enrolmentId = enrolment.EnrolmentId });
    }

    // A participant's own enrolments
    [HttpGet]
    public async Task<IActionResult> GetEnrolments([FromQuery] int userId)
    {
        var enrolments = await _context.Enrolments
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Event.EventDate)
            .Select(e => new
            {
                e.EnrolmentId,
                e.EventId,
                EventName = e.Event.EventName,
                EventDate = e.Event.EventDate,
                Location = e.Event.Location,
                CategoryName = e.Category != null ? e.Category.Name : "Unassigned",
                e.Status,
                e.EnrolmentDate
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    // The organiser's view of one event
    [HttpGet("event/{eventId:int}")]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetEnrolmentsByEvent(int eventId)
    {
        var enrolments = await _context.Enrolments
            .AsNoTracking()
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .Select(e => new EnrolmentDto
            {
                EnrolmentId = e.EnrolmentId,
                EventId = e.EventId,
                UserId = e.UserId,
                UserName = e.User.FirstName + " " + e.User.LastName,
                CategoryId = e.CategoryId,
                CategoryName = e.Category != null ? e.Category.Name : "Unassigned",
                Status = e.Status,
                EnrolmentDate = e.EnrolmentDate
            })
            .ToListAsync();

        return Ok(enrolments);
    }
}
