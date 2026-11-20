using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceMonitoringSystem.Controllers
{
    [AllowAnonymous]
    public class UserController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(AuthConstants.Scheme);

            return RedirectToPage("/Index");
        }
    }
}
