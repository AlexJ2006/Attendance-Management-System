
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

        public IActionResult Index()
        {
            var attendance = _attendanceService.GetAllAttendance();
            return View(attendance);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(
            int studentId,
            int sessionId,
            AttendanceStatus status)
        {
            if (!Enum.IsDefined(typeof(AttendanceStatus), status))
            {
                ModelState.AddModelError(
                    nameof(status), "Please select a valid attendance status.");

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

            bool recorded = _attendanceService.RecordAttendance(attendance);

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
