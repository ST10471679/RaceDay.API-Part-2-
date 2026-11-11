using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // Everything here needs a logged-in user; the user id always comes from the session
    [SessionAuthorize]
    public class EnrolmentController : Controller
    {
        private readonly ApiClient _apiClient;

        public EnrolmentController(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(int eventId, int categoryId)
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            if (categoryId <= 0)
            {
                TempData["ErrorMessage"] = "Please choose a category before enrolling.";
                return RedirectToAction("Details", "Home", new { id = eventId });
            }

            var (success, message) = await _apiClient.EnrolInEventAsync(eventId, userId.Value, categoryId, HttpContext.Session.Token());
            if (!success)
            {
                TempData["ErrorMessage"] = message;
                return RedirectToAction("Details", "Home", new { id = eventId });
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction("MyEnrolments");
        }

        [HttpGet]
        public async Task<IActionResult> MyEnrolments()
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var enrolments = await _apiClient.GetMyEnrolmentsAsync(userId.Value, HttpContext.Session.Token());
            return View(enrolments);
        }

        [HttpGet]
        public async Task<IActionResult> MyResults()
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var results = await _apiClient.GetMyResultsAsync(userId.Value, HttpContext.Session.Token());
            return View(results);
        }
    }
}
