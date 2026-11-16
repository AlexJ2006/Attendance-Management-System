using AttendanceMonitoringSystem.Models;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceMonitoringSystem.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly AttendanceService _attendanceService;

        public AttendanceController(AttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        // Course Leaders should use their restricted monitoring dashboard,
        // rather than the general attendance list.
        public IActionResult Index()
        {
            if (User.IsInRole("CourseLeader"))
            {
                return RedirectToAction(
                    "Index", "CourseLeader");
            }

            var attendance = _attendanceService.GetAllAttendance();

            return View(attendance);
        }

        // Course Leaders must not use the attendance registration page.
        public IActionResult Register()
        {
            if (User.IsInRole("CourseLeader"))
            {
                return Forbid();
            }

            return View();
        }

        // Course Leaders must not record or change attendance.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(
            int studentId,
            int sessionId,
            AttendanceStatus status)
        {
            if (User.IsInRole("CourseLeader"))
            {
                return Forbid();
            }

            if (!Enum.IsDefined(typeof(AttendanceStatus), status))
            {
                ModelState.AddModelError(
                    nameof(status),
                    "Please select a valid attendance status.");

                return View();
            }

            var attendance = new Attendance
            {
                StudentId = studentId,
                SessionId = sessionId,
                Status = status,
                RecordedAt = DateTime.UtcNow,
                RecordedByUserId = 1
            };

            bool recorded =
                _attendanceService.RecordAttendance(attendance);

            if (!recorded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Attendance could not be recorded. Check the student, session and whether attendance already exists.");

                return View();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}