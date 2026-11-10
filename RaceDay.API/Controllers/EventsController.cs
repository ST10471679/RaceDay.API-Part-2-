using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorage;

    public EventsController(ApplicationDbContext context, IBlobStorageService blobStorage)
    {
        _context = context;
        _blobStorage = blobStorage;
    }

    // ---------------------------------------------------------------- READ

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventResponseDto>>> GetEvents([FromQuery] int? organiserId = null)
    {
        var query = _context.Events.AsNoTracking().AsQueryable();

        if (organiserId.HasValue)
        {
            query = query.Where(e => e.OrganiserId == organiserId.Value);
        }

        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(e => new EventResponseDto
            {
                Id = e.EventId,
                Name = e.EventName,
                Description = e.Description,
                Date = e.EventDate,
                Location = e.Location,
                Distance = e.Distance,
                EventType = e.EventType,
                BannerImageUrl = e.BannerImageUrl,
                OrganiserId = e.OrganiserId,
                CategoryName = e.EventCategories
                    .OrderBy(c => c.EventCategoryId)
                    .Select(c => c.Name)
                    .FirstOrDefault() ?? string.Empty,
                EnrolmentCount = e.Enrolments.Count()
            })
            .ToListAsync();

        return Ok(events);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventResponseDto>> GetEvent(int id)
    {
        var raceEvent = await _context.Events
            .AsNoTracking()
            .Include(e => e.EventCategories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (raceEvent == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        return Ok(await MapEventAsync(raceEvent));
    }

    // -------------------------------------------------------------- CREATE

    // multipart/form-data so the organiser can upload a banner image with the event
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<EventResponseDto>> CreateEvent([FromForm] CreateEventDto dto)
    {
        // If a banner file was uploaded it takes priority over a typed URL
        var bannerUrl = dto.BannerImageUrl;
        if (dto.BannerImage != null && dto.BannerImage.Length > 0)
        {
            var (url, error) = await UploadBannerAsync(dto.BannerImage);
            if (error != null) return error;
            bannerUrl = url;
        }

        var distance = dto.Distance?.Trim() ?? string.Empty;

        var raceEvent = new Event
        {
            EventName = dto.EventName.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            EventDate = dto.EventDate,
            Location = dto.Location.Trim(),
            Distance = distance,
            EventType = dto.EventType?.Trim() ?? string.Empty,
            BannerImageUrl = bannerUrl,
            OrganiserId = dto.OrganiserId
        };

        // Every new event gets one starter category so people can enrol straight away.
        // The organiser can rename it or add more on the Categories page.
        raceEvent.EventCategories.Add(new EventCategory
        {
            Name = string.IsNullOrWhiteSpace(distance) ? "Standard" : distance,
            Distance = distance,
            EntryFee = 0,
            MaxParticipants = 0
        });

        _context.Events.Add(raceEvent);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = raceEvent.EventId }, await MapEventAsync(raceEvent));
    }

    // -------------------------------------------------------------- UPDATE

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<EventResponseDto>> UpdateEvent(int id, [FromForm] UpdateEventDto dto)
    {
        var raceEvent = await _context.Events
            .Include(e => e.EventCategories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (raceEvent == null) return NotFound(new { message = "Event not found." });
        if (IsNotOwner(raceEvent, dto.OrganiserId)) return NotYourEvent();

        raceEvent.EventName = dto.EventName.Trim();
        raceEvent.Description = dto.Description?.Trim() ?? string.Empty;
        raceEvent.EventDate = dto.EventDate;
        raceEvent.Location = dto.Location.Trim();
        raceEvent.Distance = dto.Distance?.Trim() ?? string.Empty;
        raceEvent.EventType = dto.EventType?.Trim() ?? string.Empty;

        if (dto.BannerImage != null && dto.BannerImage.Length > 0)
        {
            var (url, error) = await UploadBannerAsync(dto.BannerImage);
            if (error != null) return error;
            raceEvent.BannerImageUrl = url;
        }
        else if (dto.RemoveBanner)
        {
            raceEvent.BannerImageUrl = null;
        }

        // Events created before ownership existed get claimed by the first organiser who edits them
        raceEvent.OrganiserId ??= dto.OrganiserId;

        await _context.SaveChangesAsync();
        return Ok(await MapEventAsync(raceEvent));
    }

    // -------------------------------------------------------------- DELETE

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEvent(int id, [FromQuery] int? organiserId = null)
    {
        var raceEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (raceEvent == null) return NotFound(new { message = "Event not found." });
        if (IsNotOwner(raceEvent, organiserId)) return NotYourEvent();

        // Remove dependants first so no foreign key is ever violated
        _context.Results.RemoveRange(await _context.Results.Where(r => r.EventId == id).ToListAsync());
        _context.Enrolments.RemoveRange(await _context.Enrolments.Where(e => e.EventId == id).ToListAsync());
        _context.EventCategories.RemoveRange(await _context.EventCategories.Where(c => c.EventId == id).ToListAsync());
        _context.Events.Remove(raceEvent);

        await _context.SaveChangesAsync();
        return Ok(new { message = "Event deleted." });
    }

    // ---------------------------------------------------------- CATEGORIES

    [HttpPost("{eventId:int}/categories")]
    public async Task<IActionResult> AddCategory(int eventId, [FromBody] CreateEventCategoryDto dto)
    {
        var raceEvent = await _context.Events.Include(e => e.EventCategories)
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (raceEvent == null) return NotFound(new { message = "Event not found." });
        if (IsNotOwner(raceEvent, dto.OrganiserId)) return NotYourEvent();

        var name = dto.Name.Trim();
        if (raceEvent.EventCategories.Any(c => c.Name.ToLower() == name.ToLower()))
        {
            return BadRequest(new { message = $"This event already has a category called \"{name}\"." });
        }

        var category = new EventCategory
        {
            EventId = eventId,
            Name = name,
            Distance = dto.Distance?.Trim() ?? string.Empty,
            EntryFee = dto.EntryFee,
            MaxParticipants = dto.MaxParticipants
        };

        _context.EventCategories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(new { id = category.EventCategoryId, message = "Category added." });
    }

    [HttpDelete("{eventId:int}/categories/{categoryId:int}")]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId, [FromQuery] int? organiserId = null)
    {
        var raceEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (raceEvent == null) return NotFound(new { message = "Event not found." });
        if (IsNotOwner(raceEvent, organiserId)) return NotYourEvent();

        var category = await _context.EventCategories
            .FirstOrDefaultAsync(c => c.EventCategoryId == categoryId && c.EventId == eventId);
        if (category == null) return NotFound(new { message = "Category not found." });

        if (await _context.Enrolments.AnyAsync(e => e.CategoryId == categoryId))
        {
            return BadRequest(new { message = "This category already has enrolments and cannot be removed." });
        }

        _context.EventCategories.Remove(category);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Category removed." });
    }

    // ------------------------------------------------------------- HELPERS

    private static bool IsNotOwner(Event raceEvent, int? requesterId) =>
        raceEvent.OrganiserId.HasValue && raceEvent.OrganiserId != requesterId;

    private ObjectResult NotYourEvent() =>
        StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only manage events that you created." });

    private async Task<(string? Url, ActionResult? Error)> UploadBannerAsync(IFormFile file)
    {
        try
        {
            return (await _blobStorage.UploadImageAsync(file, "banners"), null);
        }
        catch (ArgumentException ex)
        {
            return (null, BadRequest(new { message = ex.Message }));
        }
        catch (InvalidOperationException)
        {
            return (null, StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Image storage is not configured on the server." }));
        }
        catch (Exception)
        {
            return (null, StatusCode(StatusCodes.Status502BadGateway,
                new { message = "The image could not be uploaded. Please try again." }));
        }
    }

    private async Task<EventResponseDto> MapEventAsync(Event e)
    {
        var counts = await _context.Enrolments
            .Where(en => en.EventId == e.EventId && en.CategoryId != null)
            .GroupBy(en => en.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync();

        var perCategory = counts.ToDictionary(x => x.CategoryId!.Value, x => x.Count);
        var totalEnrolments = await _context.Enrolments.CountAsync(en => en.EventId == e.EventId);

        var categories = e.EventCategories
            .OrderBy(c => c.EventCategoryId)
            .Select(c => new CategoryResponseDto
            {
                Id = c.EventCategoryId,
                Name = c.Name,
                Distance = c.Distance,
                EntryFee = c.EntryFee,
                MaxParticipants = c.MaxParticipants,
                EnrolledCount = perCategory.TryGetValue(c.EventCategoryId, out var n) ? n : 0
            })
            .ToList();

        return new EventResponseDto
        {
            Id = e.EventId,
            Name = e.EventName,
            Description = e.Description,
            Date = e.EventDate,
            Location = e.Location,
            Distance = e.Distance,
            EventType = e.EventType,
            BannerImageUrl = e.BannerImageUrl,
            OrganiserId = e.OrganiserId,
            CategoryName = categories.FirstOrDefault()?.Name ?? "Unassigned",
            Categories = categories,
            EnrolmentCount = totalEnrolments
        };
    }
}
