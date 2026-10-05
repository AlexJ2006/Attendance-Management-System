using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceMonitoringSystem.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");

            return RedirectToPage("/Index");
        }
    }
}