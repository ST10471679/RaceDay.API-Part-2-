using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models.DTOs
{
    // One enrolment as the organiser sees it
    public class EnrolmentDto
    {
        public int EnrolmentId { get; set; }
        public int EventId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Status { get; set; } = "Confirmed";
        public DateTime EnrolmentDate { get; set; }
    }

    // A participant's finished race
    public class ParticipantResultDto
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string FinishTime { get; set; } = string.Empty;
        public int Position { get; set; }
        public int TotalFinishers { get; set; }
    }

    // ----- results capture form -----

    public class ResultRowDto
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? CategoryName { get; set; }

        [RegularExpression(@"^\s*(\d{1,2}:)?\d{1,2}:\d{2}(\.\d{1,3})?\s*$",
            ErrorMessage = "Finish time must look like 00:45:30 (hh:mm:ss).")]
        public string? FinishTime { get; set; }

        [Range(1, 100000, ErrorMessage = "Position must be 1 or higher.")]
        public int? Position { get; set; }
    }

    public class EnterResultsViewModel
    {
        public int EventId { get; set; }
        public string? EventName { get; set; }
        public List<ResultRowDto> Rows { get; set; } = new();
    }
}
