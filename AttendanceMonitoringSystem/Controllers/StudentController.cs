
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AttendanceMonitoringSystem.Controllers
{
    public class StudentsController : Controller
    {
        private readonly StudentService _studentService;

        public StudentsController(StudentService studentService)
        {
            _studentService = studentService;
        }

        public IActionResult Index()
        {
            var students = _studentService.GetAllStudents();
            return View(students);
        }

        public IActionResult EnrolmentDetails(int studentId)
        {
            var student = _studentService.GetStudentById(studentId);

            if (student == null)
                return NotFound();

            return View(student);
        }

        [Authorize(Roles = "Adviser")]
        public IActionResult AssignedToMe()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int adviserUserId))
            {
                return Forbid();
            }

            var students = _studentService.GetStudentsForAdviser(adviserUserId);

            return View(students);
        }
    }
}
