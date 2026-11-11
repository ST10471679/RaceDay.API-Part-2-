using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RaceDay.Web.Models;
using RaceDay.Web.Models.DTOs;

namespace RaceDay.Web.Services
{
    /// <summary>
    /// The MVC project's ONLY way of getting data: every method calls the RaceDay API.
    /// Nothing here ever throws to the page - failures come back as empty lists, null or a friendly message.
    /// </summary>
    public class ApiClient
    {
        private const string Unreachable = "Could not reach the server. Please try again.";

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("RaceDayApi");
        }

        private void SetToken(string? token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                string.IsNullOrWhiteSpace(token) ? null : new AuthenticationHeaderValue("Bearer", token);
        }

        // Pulls a friendly { "message": "..." } out of an API error; never shows raw error bodies
        private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, string fallback)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("message", out var msg) &&
                    msg.ValueKind == JsonValueKind.String)
                {
                    return msg.GetString() ?? fallback;
                }
            }
            catch { /* not JSON - fall through */ }

            return fallback;
        }

        private static async Task<string> ReadSuccessMessageAsync(HttpResponseMessage response, string fallback)
        {
            return await ReadErrorMessageAsync(response, fallback);
        }

        // ------------------------------------------------------------ AUTH

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(loginDto), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/auth/login", content);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AuthResponseDto>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return null;
            }
        }

        public async Task<(bool Success, string Message)> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(registerDto), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/auth/register", content);

                return response.IsSuccessStatusCode
                    ? (true, "Account created. Please log in.")
                    : (false, await ReadErrorMessageAsync(response, "Registration failed. Please check your details and try again."));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Register error: {ex.Message}");
                return (false, Unreachable);
            }
        }

        // ---------------------------------------------------------- EVENTS

        public async Task<List<EventDto>> GetEventsAsync(string token)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.GetAsync("api/events");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<EventDto>>(_jsonOptions) ?? new List<EventDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading events: {ex.Message}");
            }

            return new List<EventDto>();
        }

        public async Task<EventDto?> GetEventByIdAsync(int id, string token)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.GetAsync($"api/events/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<EventDto>(_jsonOptions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching event details: {ex.Message}");
            }

            return null;
        }

        public async Task<List<EventDto>> GetEventsByOrganiserAsync(string token, int organiserId)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.GetAsync($"api/events?organiserId={organiserId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<EventDto>>(_jsonOptions) ?? new List<EventDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading organiser events: {ex.Message}");
            }

            return new List<EventDto>();
        }

        public async Task<(bool Success, string Message, int EventId)> CreateEventAsync(CreateEventDto dto, string token)
        {
            try
            {
                SetToken(token);

                // multipart/form-data so the banner image can travel with the event fields
                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(dto.EventName ?? ""), "EventName");
                form.Add(new StringContent(dto.Description ?? ""), "Description");
                form.Add(new StringContent(dto.EventDate.ToString("s")), "EventDate");
                form.Add(new StringContent(dto.Location ?? ""), "Location");
                form.Add(new StringContent(dto.Distance ?? ""), "Distance");
                form.Add(new StringContent(dto.EventType ?? ""), "EventType");

                if (dto.OrganiserId.HasValue)
                    form.Add(new StringContent(dto.OrganiserId.Value.ToString()), "OrganiserId");

                if (!string.IsNullOrWhiteSpace(dto.BannerImageUrl))
                    form.Add(new StringContent(dto.BannerImageUrl), "BannerImageUrl");

                AddFile(form, dto.BannerImage, "BannerImage");

                var response = await _httpClient.PostAsync("api/events", form);

                if (!response.IsSuccessStatusCode)
                {
                    return (false, await ReadErrorMessageAsync(response, "The event could not be created."), 0);
                }

                var created = await response.Content.ReadFromJsonAsync<EventDto>(_jsonOptions);
                return (true, "Event created successfully!", created?.Id ?? 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating event: {ex.Message}");
                return (false, Unreachable, 0);
            }
        }

        public async Task<(bool Success, string Message)> UpdateEventAsync(EditEventDto dto, int organiserId, string token)
        {
            try
            {
                SetToken(token);

                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(dto.EventName ?? ""), "EventName");
                form.Add(new StringContent(dto.Description ?? ""), "Description");
                form.Add(new StringContent(dto.EventDate.ToString("s")), "EventDate");
                form.Add(new StringContent(dto.Location ?? ""), "Location");
                form.Add(new StringContent(dto.Distance ?? ""), "Distance");
                form.Add(new StringContent(dto.EventType ?? ""), "EventType");
                form.Add(new StringContent(dto.RemoveBanner ? "true" : "false"), "RemoveBanner");
                form.Add(new StringContent(organiserId.ToString()), "OrganiserId");

                AddFile(form, dto.BannerImage, "BannerImage");

                var response = await _httpClient.PutAsync($"api/events/{dto.Id}", form);

                return response.IsSuccessStatusCode
                    ? (true, "Event updated successfully!")
                    : (false, await ReadErrorMessageAsync(response, "The event could not be updated."));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating event: {ex.Message}");
                return (false, Unreachable);
            }
        }

        public async Task<(bool Success, string Message)> DeleteEventAsync(int eventId, int organiserId, string token)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.DeleteAsync($"api/events/{eventId}?organiserId={organiserId}");

                return response.IsSuccessStatusCode
                    ? (true, "Event deleted.")
                    : (false, await ReadErrorMessageAsync(response, "The event could not be deleted."));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting event: {ex.Message}");
                return (false, Unreachable);
            }
        }

        // ------------------------------------------------------ CATEGORIES

        public async Task<(bool Success, string Message)> AddCategoryAsync(int eventId, CreateCategoryDto dto, int organiserId, string token)
        {
            try
            {
                SetToken(token);

                var body = new
                {
                    dto.Name,
                    Distance = dto.Distance ?? string.Empty,
                    dto.EntryFee,
                    dto.MaxParticipants,
                    OrganiserId = organiserId
                };

                var response = await _httpClient.PostAsJsonAsync($"api/events/{eventId}/categories", body);

                return response.IsSuccessStatusCode
                    ? (true, "Category added.")
                    : (false, await ReadErrorMessageAsync(response, "The category could not be added."));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding category: {ex.Message}");
                return (false, Unreachable);
            }
        }

        public async Task<(bool Success, string Message)> DeleteCategoryAsync(int eventId, int categoryId, int organiserId, string token)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.DeleteAsync($"api/events/{eventId}/categories/{categoryId}?organiserId={organiserId}");

                return response.IsSuccessStatusCode
                    ? (true, "Category removed.")
                    : (false, await ReadErrorMessageAsync(response, "The category could not be removed."));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing category: {ex.Message}");
                return (false, Unreachable);
            }
        }

        // ------------------------------------------------------ ENROLMENTS

        public async Task<(bool Success, string Message)> EnrolInEventAsync(int eventId, int userId, int categoryId, string token)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.PostAsync(
                    $"api/enrolments?eventId={eventId}&userId={userId}&categoryId={categoryId}", null);

                return response.IsSuccessStatusCode
                    ? (true, await ReadSuccessMessageAsync(response, "Successfully enrolled in the event!"))
                    : (false, await ReadErrorMessageAsync(response, "Unable to process your enrolment."));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enrolling: {ex.Message}");
                return (false, Unreachable);
            }
        }

        public async Task<List<EnrolmentViewModel>> GetMyEnrolmentsAsync(int userId, string token)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.GetAsync($"api/enrolments?userId={userId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<EnrolmentViewModel>>(_jsonOptions)
                           ?? new List<EnrolmentViewModel>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching enrolments: {ex.Message}");
            }

            return new List<EnrolmentViewModel>();
        }

        public async Task<List<EnrolmentDto>> GetEnrolmentsByEventAsync(string token, int eventId)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.GetAsync($"api/enrolments/event/{eventId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>(_jsonOptions)
                           ?? new List<EnrolmentDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching event enrolments: {ex.Message}");
            }

            return new List<EnrolmentDto>();
        }

        // --------------------------------------------------------- RESULTS

        public async Task<List<ResultRowDto>> GetEventResultsAsync(string token, int eventId)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.GetAsync($"api/results/event/{eventId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<ResultRowDto>>(_jsonOptions)
                           ?? new List<ResultRowDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching event results: {ex.Message}");
            }

            return new List<ResultRowDto>();
        }

        public async Task<(bool Success, string Message)> SaveResultsAsync(int eventId, int organiserId, IEnumerable<ResultRowDto> rows, string token)
        {
            try
            {
                SetToken(token);

                var body = new
                {
                    OrganiserId = organiserId,
                    Results = rows.Select(r => new { r.UserId, r.FinishTime, r.Position })
                };

                var response = await _httpClient.PostAsJsonAsync($"api/results/event/{eventId}", body);

                return response.IsSuccessStatusCode
                    ? (true, await ReadSuccessMessageAsync(response, "Results saved."))
                    : (false, await ReadErrorMessageAsync(response, "The results could not be saved."));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving results: {ex.Message}");
                return (false, Unreachable);
            }
        }

        public async Task<List<ParticipantResultDto>> GetMyResultsAsync(int userId, string token)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.GetAsync($"api/results/my-results?userId={userId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<ParticipantResultDto>>(_jsonOptions)
                           ?? new List<ParticipantResultDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching results: {ex.Message}");
            }

            return new List<ParticipantResultDto>();
        }

        // --------------------------------------------------------- PROFILE

        public async Task<UserProfileDto?> GetUserProfileAsync(int userId, string token)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.GetAsync($"api/users/{userId}");
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<UserProfileDto>(_jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching profile: {ex.Message}");
                return null;
            }
        }

        public async Task<(bool Success, string Message)> UpdateUserProfileAsync(int userId, UpdateProfileDto model, string token)
        {
            try
            {
                SetToken(token);

                using var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(model.UserName ?? string.Empty), "UserName");
                AddFile(formData, model.ProfilePicture, "ProfilePicture");

                var response = await _httpClient.PutAsync($"api/users/{userId}", formData);
                if (response.IsSuccessStatusCode) return (true, "Profile updated successfully!");

                return (false, await ReadErrorMessageAsync(response, "Failed to update profile."));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profile: {ex.Message}");
                return (false, Unreachable);
            }
        }

        // ---------------------------------------------------------- HELPERS

        private static void AddFile(MultipartFormDataContent form, IFormFile? file, string fieldName)
        {
            if (file == null || file.Length == 0) return;

            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            form.Add(fileContent, fieldName, file.FileName);
        }
    }
}
