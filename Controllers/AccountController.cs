using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.DTOs;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApiClient _apiClient;

        public AccountController(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.IsLoggedIn()) return RedirectToHome();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _apiClient.LoginAsync(model);
            if (result == null || string.IsNullOrEmpty(result.Token))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            // Save user info and token to session
            HttpContext.Session.SetString(SessionHelper.TokenKey, result.Token);
            HttpContext.Session.SetString(SessionHelper.UserIdKey, result.UserId.ToString());
            HttpContext.Session.SetString(SessionHelper.UserNameKey, result.UserName ?? string.Empty);
            HttpContext.Session.SetString(SessionHelper.UserRoleKey, result.RoleName ?? string.Empty);

            return RedirectToHome();
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.IsLoggedIn()) return RedirectToHome();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            if (!ModelState.IsValid) return View(model);

            var (success, message) = await _apiClient.RegisterAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction("Login");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        [SessionAuthorize]
        [HttpGet("Account/Profile")]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId == null) return RedirectToAction("Login");

            var profile = await _apiClient.GetUserProfileAsync(userId.Value, HttpContext.Session.Token());
            if (profile == null)
            {
                TempData["ErrorMessage"] = "We could not load your profile right now. Please try again.";
                return RedirectToAction("Index", "Home");
            }

            return View(profile);
        }

        [SessionAuthorize]
        [HttpPost("Account/Profile")]
        public async Task<IActionResult> Profile(UpdateProfileDto model)
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId == null) return RedirectToAction("Login");
            var token = HttpContext.Session.Token();

            if (string.IsNullOrWhiteSpace(model.UserName))
            {
                var current = await _apiClient.GetUserProfileAsync(userId.Value, token) ?? new UserProfileDto { Id = userId.Value };
                ModelState.AddModelError("", "Name cannot be empty.");
                return View(current);
            }

            var (success, message) = await _apiClient.UpdateUserProfileAsync(userId.Value, model, token);
            if (!success)
            {
                // Reload the saved profile so the page still shows the email and current picture
                var profile = await _apiClient.GetUserProfileAsync(userId.Value, token)
                              ?? new UserProfileDto { Id = userId.Value };
                profile.UserName = model.UserName;
                ModelState.AddModelError("", message);
                return View(profile);
            }

            // Keep the navbar greeting in sync with the new name
            HttpContext.Session.SetString(SessionHelper.UserNameKey, model.UserName.Trim());

            TempData["SuccessMessage"] = message;
            return RedirectToAction("Profile");
        }

        // Organisers land on their dashboard, participants on the event list
        private IActionResult RedirectToHome() =>
            HttpContext.Session.IsOrganiser()
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Index", "Home");
    }
}
