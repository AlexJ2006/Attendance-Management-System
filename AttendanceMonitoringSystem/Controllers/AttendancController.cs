using AttendanceMonitoringSystem.Models;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceMonitoringSystem.Controllers
{
    // Read-only list of every attendance record, for Administrators only.
    // Attendance is recorded by students (attendance code) or by the
    // assigned Tutor through the session register, never from this page.
    [Authorize(Roles = RoleNames.Administrator)]
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
    }
}
