using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrolmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EnrolmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> EnrolUser([FromQuery] int userId, [FromQuery] int eventId)
    {
        var exists = await _context.Enrolments
            .AnyAsync(e => e.UserId == userId && e.EventId == eventId);

        if (exists)
        {
            return BadRequest(new { message = "User is already enrolled in this event." });
        }

        var enrolment = new Enrolment
        {
            UserId = userId,
            EventId = eventId,
            EnrolmentDate = DateTime.UtcNow
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Successfully enrolled in event.", enrolmentId = enrolment.EnrolmentId });
    }
}