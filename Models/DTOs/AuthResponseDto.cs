using System.Text.Json.Serialization;

namespace RaceDay.Web.Models.DTOs
{
    public class AuthResponseDto
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("userName")]
        public string UserName { get; set; } = string.Empty;

        [JsonPropertyName("roleName")]
        public string RoleName { get; set; } = string.Empty;

        // Convenience property for layout consistency
        public string Role => RoleName;
        
    }
}