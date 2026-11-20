using System.Security.Claims;
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Controllers
{
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AccessScopeService _scopeService;

        public StudentsController(
            ApplicationDbContext context,
            AccessScopeService scopeService)
        {
            _context = context;
            _scopeService = scopeService;
        }

        // Staff see only the students inside their academic scope (T08).
        // Students cannot open this page at all.
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = RoleNames.AllStaff)]
        public async Task<IActionResult> Index()
        {
            var scope = await _scopeService.GetStaffScopeAsync(User);
            var model = await BuildListAsync(
                ScopedStudents(scope),
                scope.All ? "All Students" : "My Students",
                "Students on the modules, courses or advisee lists you are responsible for.");

            return View("List", model);
        }

        // Changing the studentId in the URL cannot reveal a student outside
        // the user's scope; the request is refused instead (T07, T08).
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = RoleNames.AllStaff)]
        public async Task<IActionResult> EnrolmentDetails(int studentId)
        {
            var scope = await _scopeService.GetStaffScopeAsync(User);

            var exists = await _context.Students.AnyAsync(s => s.StudentId == studentId);
            if (!exists)
            {
                return NotFound();
            }

            var student = await ScopedStudents(scope)
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                return Forbid();
            }

            // Only show the records this member of staff is allowed to see.
            var records = await AccessScopeService.ApplyScope(
                    _context.Attendances.AsNoTracking(), scope)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.Session.StartTime)
                .Select(a => new StudentAttendanceRecordViewModel
                {
                    SessionDate = a.Session.StartTime,
                    ModuleCode = a.Session.ModuleOccurrence.Module.ModuleCode,
                    ModuleName = a.Session.ModuleOccurrence.Module.ModuleName,
                    SessionType = a.Session.SessionType,
                    Status = a.Status
                })
                .ToListAsync();

            return View(new StudentDetailsViewModel
            {
                Student = student,
                Records = records,
                Percentage = AttendanceCalculator.Percentage(records.Select(r => r.Status))
            });
        }

        private IQueryable<Models.Student> ScopedStudents(AccessScope scope)
        {
            var query = _context.Students.AsNoTracking();

            if (scope.All)
            {
                return query;
            }

            var occurrenceIds = scope.OccurrenceIds.ToList();
            var studentIds = scope.StudentIds.ToList();

            return query.Where(s =>
                studentIds.Contains(s.StudentId) ||
                _context.ModuleOccurrences.Any(mo =>
                    occurrenceIds.Contains(mo.ModuleOccurrenceId) &&
                    mo.Module.CourseId == s.CourseId));
        }

        // Academic Advisers see the students assigned to them.
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = RoleNames.Adviser)]
        public async Task<IActionResult> AssignedToMe()
        {
            var adviserUserId = AccessScopeService.GetUserId(User);

            if (adviserUserId == null)
            {
                return Forbid();
            }

            var advisees = _context.Students.Where(s =>
                _context.AdviserAssignments.Any(a =>
                    a.AdviserUserId == adviserUserId.Value &&
                    a.StudentId == s.StudentId));

            var model = await BuildListAsync(
                advisees,
                "My Advisees",
                "Students assigned to you as their Academic Adviser. Students below 75% are highlighted.");

            return View("List", model);
        }

        private async Task<StudentListViewModel> BuildListAsync(
            IQueryable<Models.Student> students, string title, string description)
        {
            var rows = await students
                .AsNoTracking()
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Select(s => new StudentListRowViewModel
                {
                    StudentId = s.StudentId,
                    Name = s.FirstName + " " + s.LastName,
                    Email = s.Email,
                    CourseName = s.Course.CourseName,
                    YearOfStudy = s.YearOfStudy,
                    Statuses = s.Attendances.Select(a => a.Status).ToList()
                })
                .ToListAsync();

            return new StudentListViewModel
            {
                Title = title,
                Description = description,
                Students = rows
            };
        }
    }
}
namespace AttendanceMonitoringSystem.Controllers
{
    public class StudentListViewModel
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public List<StudentListRowViewModel> Students { get; set; } = new();
    }

    public class StudentListRowViewModel
    {
        public int StudentId { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int YearOfStudy { get; set; }
        public List<AttendanceStatus> Statuses { get; set; } = new();
        public double Percentage => AttendanceCalculator.Percentage(Statuses);
        public bool HasRecords => Statuses.Any(AttendanceCalculator.CountsTowardsPercentage);
    }

    public class StudentDetailsViewModel
    {
        public Student Student { get; set; } = null!;
        public List<StudentAttendanceRecordViewModel> Records { get; set; } = new();
        public double Percentage { get; set; }
    }

    public class StudentAttendanceRecordViewModel
    {
        public DateTime SessionDate { get; set; }
        public string ModuleCode { get; set; } = "";
        public string ModuleName { get; set; } = "";
        public string SessionType { get; set; } = "";
        public AttendanceStatus Status { get; set; }
    }
}
