using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.DTOs;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // Organisers only. Participants are sent to the "access denied" page.
    [SessionAuthorize(Role = "Organiser")]
    public class AdminController : Controller
    {
        private readonly ApiClient _apiClient;

        public AdminController(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private string Token => HttpContext.Session.Token();
        private int OrganiserId => HttpContext.Session.GetUserId() ?? 0;

        // Loads an event and makes sure it belongs to the logged-in organiser
        private async Task<(EventDto? Event, IActionResult? Failure)> LoadOwnedEventAsync(int id)
        {
            var raceEvent = await _apiClient.GetEventByIdAsync(id, Token);
            if (raceEvent == null) return (null, NotFound());

            if (raceEvent.OrganiserId.HasValue && raceEvent.OrganiserId != OrganiserId)
            {
                return (null, RedirectToAction("AccessDenied", "Account"));
            }

            return (raceEvent, null);
        }

        // ------------------------------------------------------- DASHBOARD

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var events = await _apiClient.GetEventsByOrganiserAsync(Token, OrganiserId);
            return View(events);
        }

        // ----------------------------------------------------- CREATE EVENT

        [HttpGet]
        public IActionResult CreateEvent() => View(new CreateEventDto());

        [HttpPost]
        public async Task<IActionResult> CreateEvent(CreateEventDto model)
        {
            if (!ModelState.IsValid) return View(model);

            // Record who created the event so it shows on this organiser's dashboard
            model.OrganiserId = OrganiserId;

            var result = await _apiClient.CreateEventAsync(model, Token);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message + " Add the race categories people can enter below.";
                return result.EventId > 0
                    ? RedirectToAction("ManageCategories", new { id = result.EventId })
                    : RedirectToAction("Index");
            }

            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        // ------------------------------------------------------ EDIT EVENT

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var (raceEvent, failure) = await LoadOwnedEventAsync(id);
            if (failure != null) return failure;

            return View(new EditEventDto
            {
                Id = raceEvent!.Id,
                EventName = raceEvent.Name,
                Description = raceEvent.Description,
                EventDate = raceEvent.Date,
                Location = raceEvent.Location,
                Distance = raceEvent.Distance,
                EventType = raceEvent.EventType,
                ExistingBannerUrl = raceEvent.BannerImageUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(EditEventDto model)
        {
            var (raceEvent, failure) = await LoadOwnedEventAsync(model.Id);
            if (failure != null) return failure;

            // The preview URL is not posted back, so take it from the saved event
            model.ExistingBannerUrl = raceEvent!.BannerImageUrl;

            if (!ModelState.IsValid) return View(model);

            var (success, message) = await _apiClient.UpdateEventAsync(model, OrganiserId, Token);
            if (!success)
            {
                ModelState.AddModelError("", message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction("Index");
        }

        // ---------------------------------------------------- DELETE EVENT

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var (_, failure) = await LoadOwnedEventAsync(id);
            if (failure != null) return failure;

            var (success, message) = await _apiClient.DeleteEventAsync(id, OrganiserId, Token);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction("Index");
        }

        // ---------------------------------------------------- CATEGORIES

        [HttpGet]
        public async Task<IActionResult> ManageCategories(int id)
        {
            var (raceEvent, failure) = await LoadOwnedEventAsync(id);
            if (failure != null) return failure;

            return View(new ManageCategoriesViewModel
            {
                Event = raceEvent!,
                NewCategory = new CreateCategoryDto { EventId = id }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory([Bind(Prefix = "NewCategory")] CreateCategoryDto model)
        {
            var (raceEvent, failure) = await LoadOwnedEventAsync(model.EventId);
            if (failure != null) return failure;

            if (!ModelState.IsValid)
            {
                return View("ManageCategories", new ManageCategoriesViewModel { Event = raceEvent!, NewCategory = model });
            }

            var (success, message) = await _apiClient.AddCategoryAsync(model.EventId, model, OrganiserId, Token);
            if (!success)
            {
                ModelState.AddModelError("", message);
                return View("ManageCategories", new ManageCategoriesViewModel { Event = raceEvent!, NewCategory = model });
            }

            TempData["SuccessMessage"] = $"Category \"{model.Name}\" added.";
            return RedirectToAction("ManageCategories", new { id = model.EventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
        {
            var (_, failure) = await LoadOwnedEventAsync(eventId);
            if (failure != null) return failure;

            var (success, message) = await _apiClient.DeleteCategoryAsync(eventId, categoryId, OrganiserId, Token);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction("ManageCategories", new { id = eventId });
        }

        // ------------------------------------------------------ ENROLMENTS

        [HttpGet]
        public async Task<IActionResult> ViewEnrolments(int id)
        {
            var (raceEvent, failure) = await LoadOwnedEventAsync(id);
            if (failure != null) return failure;

            ViewBag.EventId = id;
            ViewBag.EventName = raceEvent!.Name;

            var enrolments = await _apiClient.GetEnrolmentsByEventAsync(Token, id);
            return View(enrolments);
        }

        // --------------------------------------------------------- RESULTS

        [HttpGet]
        public async Task<IActionResult> EnterResults(int id)
        {
            var (raceEvent, failure) = await LoadOwnedEventAsync(id);
            if (failure != null) return failure;

            return View(await BuildResultsModelAsync(raceEvent!));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveResults(EnterResultsViewModel model)
        {
            var (raceEvent, failure) = await LoadOwnedEventAsync(model.EventId);
            if (failure != null) return failure;

            model.EventName = raceEvent!.Name;

            if (!ModelState.IsValid) return View("EnterResults", model);

            var (success, message) = await _apiClient.SaveResultsAsync(model.EventId, OrganiserId, model.Rows, Token);
            if (!success)
            {
                ModelState.AddModelError("", message);
                return View("EnterResults", model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction("Index");
        }

        // One row per enrolled participant, pre-filled with any results already saved
        private async Task<EnterResultsViewModel> BuildResultsModelAsync(EventDto raceEvent)
        {
            var enrolments = await _apiClient.GetEnrolmentsByEventAsync(Token, raceEvent.Id);
            var existing = (await _apiClient.GetEventResultsAsync(Token, raceEvent.Id))
                .ToDictionary(r => r.UserId);

            return new EnterResultsViewModel
            {
                EventId = raceEvent.Id,
                EventName = raceEvent.Name,
                Rows = enrolments
                    .Where(e => !e.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                    .Select(e => new ResultRowDto
                    {
                        UserId = e.UserId,
                        UserName = e.UserName,
                        CategoryName = e.CategoryName,
                        FinishTime = existing.TryGetValue(e.UserId, out var r) ? r.FinishTime : null,
                        Position = existing.TryGetValue(e.UserId, out var r2) ? r2.Position : null
                    })
                    .ToList()
            };
        }
    }
}
