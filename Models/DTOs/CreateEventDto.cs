using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models.DTOs
{
    public class CreateEventDto
    {
        [Required]
        public string EventName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; } = DateTime.Now.AddDays(7);

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string Distance { get; set; } = string.Empty;

        [Required]
        public string EventType { get; set; } = string.Empty;

        public string? BannerImageUrl { get; set; }

        // Banner file picked on the form; forwarded to the API, which stores it in Azure Blob Storage
        public IFormFile? BannerImage { get; set; }

        public int? OrganiserId { get; set; }
    }

    public class EditEventDto
    {
        public int Id { get; set; }

        [Required]
        public string EventName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string Distance { get; set; } = string.Empty;

        [Required]
        public string EventType { get; set; } = string.Empty;

        // Shown as a preview; not posted to the API
        public string? ExistingBannerUrl { get; set; }

        // New banner (optional) and "remove current banner" tick box
        public IFormFile? BannerImage { get; set; }
        public bool RemoveBanner { get; set; }
    }

    public class CreateCategoryDto
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Distance { get; set; }

        [Range(0, 1000000, ErrorMessage = "Entry fee must be zero or more.")]
        public decimal EntryFee { get; set; }

        // 0 = unlimited
        [Range(0, 100000, ErrorMessage = "Maximum participants must be zero or more.")]
        public int MaxParticipants { get; set; }
    }

    public class ManageCategoriesViewModel
    {
        public EventDto Event { get; set; } = new();
        public CreateCategoryDto NewCategory { get; set; } = new();
    }
}
