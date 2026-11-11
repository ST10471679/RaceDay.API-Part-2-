namespace RaceDay.Web.Models.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Distance { get; set; } = string.Empty;
        public decimal EntryFee { get; set; }

        // 0 = unlimited
        public int MaxParticipants { get; set; }
        public int EnrolledCount { get; set; }

        public bool IsFull => MaxParticipants > 0 && EnrolledCount >= MaxParticipants;
    }
}
