using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class EventResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Distance { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<CategoryResponseDto> Categories { get; set; } = new();
        public int? OrganiserId { get; set; }

        // Total enrolments for this event (shown on the organiser dashboard)
        public int EnrolmentCount { get; set; }
    }

    public class CategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Distance { get; set; } = string.Empty;
        public decimal EntryFee { get; set; }
        public int MaxParticipants { get; set; }
        public int EnrolledCount { get; set; }
    }

    public class CreateEventDto
    {
        [Required]
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        [Required]
        public string Location { get; set; } = string.Empty;
        public string? Distance { get; set; }
        public string? EventType { get; set; }
        public string? BannerImageUrl { get; set; }
        public IFormFile? BannerImage { get; set; }
        public int? OrganiserId { get; set; }
    }

    // PUT api/events/{id} (multipart/form-data so the banner can be replaced)
    public class UpdateEventDto
    {
        [Required]
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        [Required]
        public string Location { get; set; } = string.Empty;
        public string? Distance { get; set; }
        public string? EventType { get; set; }
        public IFormFile? BannerImage { get; set; }
        public bool RemoveBanner { get; set; }

        // The organiser making the change (must own the event)
        public int? OrganiserId { get; set; }
    }

    public class CreateEventCategoryDto
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Distance { get; set; }

        [Range(0, 1000000)]
        public decimal EntryFee { get; set; }

        // 0 = unlimited
        [Range(0, 100000)]
        public int MaxParticipants { get; set; }

        public int? OrganiserId { get; set; }
    }
}
