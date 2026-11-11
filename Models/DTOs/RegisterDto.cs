using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models.DTOs
{
    public class RegisterDto
    {
        [Required, StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        // 1 = Organiser, 2 = Participant (matches the roles seeded by the API)
        public int RoleId { get; set; } = 2;
    }
}
