namespace RaceDay.Web.Models.DTOs
{
    public class EventDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Distance { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public string? BannerUrl => BannerImageUrl;
        public int? OrganiserId { get; set; }
        public int EnrolmentCount { get; set; }
        public List<CategoryDto> Categories { get; set; } = new();
    }
}
