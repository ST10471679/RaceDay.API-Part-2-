using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiClient _apiClient;

        public HomeController(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchString, string? eventType)
        {
            var events = await _apiClient.GetEventsAsync(HttpContext.Session.Token());

            var upcomingEvents = events.Where(e => e.Date >= DateTime.Now);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                upcomingEvents = upcomingEvents.Where(e =>
                    e.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    e.Location.Contains(searchString, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(eventType) && eventType != "All")
            {
                upcomingEvents = upcomingEvents.Where(e =>
                    e.EventType.Equals(eventType, StringComparison.OrdinalIgnoreCase));
            }

            ViewBag.SearchString = searchString;
            ViewBag.EventType = eventType;

            return View(upcomingEvents.OrderBy(e => e.Date).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var raceEvent = await _apiClient.GetEventByIdAsync(id, HttpContext.Session.Token());
            if (raceEvent == null)
            {
                return NotFound();
            }

            return View(raceEvent);
        }

        [HttpGet]
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
