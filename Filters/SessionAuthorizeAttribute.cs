using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Helpers;

namespace RaceDay.Web.Filters
{
    /// <summary>
    /// Session-based access control.
    ///   [SessionAuthorize]                     any logged-in user
    ///   [SessionAuthorize(Role = "Organiser")] organisers only
    /// Anonymous visitors go to the login page; the wrong role goes to an "access denied" page.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        public string? Role { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            if (!session.IsLoggedIn())
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var allowed = Role switch
            {
                "Organiser" => session.IsOrganiser(),
                "Participant" => session.IsParticipant(),
                _ => true
            };

            if (!allowed)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
        }
    }
}
