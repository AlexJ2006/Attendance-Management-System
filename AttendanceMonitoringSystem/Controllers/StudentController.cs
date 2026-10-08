using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Mvc;

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
            {
                return NotFound();
            }

            return View(student);
        }
    }
}