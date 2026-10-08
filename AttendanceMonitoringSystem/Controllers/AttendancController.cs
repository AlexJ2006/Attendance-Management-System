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
        public IActionResult Register(int studentId, int sessionId, string status)
        {
            var attendance = new Attendance
            {
                AttendanceId = _attendanceService.GetAllAttendance().Count + 1,
                StudentId = studentId,
                SessionId = sessionId,
                Status = status,
                RecordedAt = DateTime.Now,
                RecordedByUserId = 1
            };

            _attendanceService.RecordAttendance(attendance);

            return View();
        }
    }
}