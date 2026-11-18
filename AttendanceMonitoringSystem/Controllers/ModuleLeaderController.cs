
using System.Security.Claims;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceMonitoringSystem.Controllers
{
    // Only users with the ModuleLeader role can access these pages.
    [Authorize(Roles = "ModuleLeader")]
    public class ModuleLeaderController : Controller
    {
        private readonly ModuleLeaderService _moduleLeaderService;

        // Inject the Module Leader service.
        public ModuleLeaderController(
            ModuleLeaderService moduleLeaderService)
        {
            _moduleLeaderService = moduleLeaderService;
        }

        // Displays the dashboard for the signed-in Module Leader.
        public async Task<IActionResult> Index()
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Stop if the authenticated user's ID is unavailable.
            if (!int.TryParse(userIdValue, out int userId))
            {
                return Forbid();
            }

            // Retrieve only the modules and attendance information
            // assigned to this Module Leader.
            var model =
                await _moduleLeaderService.GetDashboardAsync(userId);

            return View(model);
        }
    }
}
