
using System.Security.Claims;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceMonitoringSystem.Controllers
{
    // Only users with the CourseLeader role can access these pages.
    [Authorize(Roles = "CourseLeader")]
    public class CourseLeaderController : Controller
    {
        private readonly CourseLeaderService _courseLeaderService;

        public CourseLeaderController(
            CourseLeaderService courseLeaderService)
        {
            _courseLeaderService = courseLeaderService;
        }

        // Displays the dashboard for the signed-in Course Leader.
        public async Task<IActionResult> Index()
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Do not continue if the authenticated user's ID is unavailable.
            if (!int.TryParse(userIdValue, out int userId))
            {
                return Forbid();
            }

            var model =
                await _courseLeaderService.GetDashboardAsync(userId);

            return View(model);
        }

        // Displays a course only when it is assigned to this Course Leader.
        public async Task<IActionResult> Course(int id)
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
            {
                return Forbid();
            }

            var model =
                await _courseLeaderService.GetCourseAsync(userId, id);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        // Displays a module only when its course is assigned to this Course Leader.
        public async Task<IActionResult> Module(int id)
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
            {
                return Forbid();
            }

            // The service checks whether the module belongs to an assigned course.
            var model =
                await _courseLeaderService.GetModuleAsync(userId, id);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        // Displays a student's attendance only when their course is assigned
        // to the signed-in Course Leader.
        public async Task<IActionResult> Student(int id)
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
            {
                return Forbid();
            }

            var model =
                await _courseLeaderService.GetStudentAttendanceAsync(
                    userId, id);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }
    }
}
