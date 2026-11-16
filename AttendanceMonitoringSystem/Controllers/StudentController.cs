using System.Security.Claims;
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Controllers
{
    public class StudentsController : Controller
    {
        private readonly StudentService _studentService;
        private readonly ApplicationDbContext _context;

        public StudentsController(
            StudentService studentService,
            ApplicationDbContext context)
        {
            _studentService = studentService;
            _context = context;
        }

        // Course Leaders see only students belonging to their assigned courses.
        // Other existing users retain the current student list behaviour.
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("CourseLeader"))
            {
                var userIdValue =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIdValue, out int userId))
                {
                    return Forbid();
                }

                var assignedCourseIds =
                    await _context.CourseLeaderAssignments
                        .AsNoTracking()
                        .Where(a => a.CourseLeaderUserId == userId)
                        .Select(a => a.CourseId)
                        .Distinct()
                        .ToListAsync();

                var assignedStudents = await _context.Students
                    .AsNoTracking()
                    .Where(s => assignedCourseIds.Contains(s.CourseId))
                    .Include(s => s.Course)
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToListAsync();

                return View(assignedStudents);
            }

            var students = _studentService.GetAllStudents();

            return View(students);
        }

        // Prevent Course Leaders from opening an enrolment record belonging
        // to a student outside their assigned courses.
        public async Task<IActionResult> EnrolmentDetails(int studentId)
        {
            if (User.IsInRole("CourseLeader"))
            {
                var userIdValue =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIdValue, out int userId))
                {
                    return Forbid();
                }

                var student = await _context.Students
                    .AsNoTracking()
                    .Include(s => s.Course)
                    .FirstOrDefaultAsync(s =>
                        s.StudentId == studentId &&
                        _context.CourseLeaderAssignments.Any(a =>
                            a.CourseLeaderUserId == userId &&
                            a.CourseId == s.CourseId));

                if (student == null)
                {
                    return NotFound();
                }

                return View(student);
            }

            var existingStudent =
                _studentService.GetStudentById(studentId);

            if (existingStudent == null)
            {
                return NotFound();
            }

            return View(existingStudent);
        }

        // This existing page remains restricted to Advisers.
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Adviser")]
        public IActionResult AssignedToMe()
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int adviserUserId))
            {
                return Forbid();
            }

            var students =
                _studentService.GetStudentsForAdviser(adviserUserId);

            return View(students);
        }
    }
}