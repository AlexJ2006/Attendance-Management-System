
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AttendanceMonitoringSystem.Controllers;

// Only users with the CourseLeader role can access these reports.
[Authorize(Roles = "CourseLeader")]
public class CourseLeaderReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public CourseLeaderReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Displays attendance reports for courses assigned to the signed-in Course Leader.
    public async Task<IActionResult> Index(
        DateTime? startDate,
        DateTime? endDate,
        int? courseId,
        int? moduleId)
    {
        // Identify the currently signed-in Course Leader.
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out int userId))
        {
            return Forbid();
        }

        // Retrieve only the course IDs assigned to this Course Leader.
        var assignedCourseIds = await _context.CourseLeaderAssignments
            .Where(a => a.CourseLeaderUserId == userId)
            .Select(a => a.CourseId)
            .Distinct()
            .ToListAsync();

        // Prevent a Course Leader from requesting a course outside their assignments.
        if (courseId.HasValue && !assignedCourseIds.Contains(courseId.Value))
        {
            return Forbid();
        }

        // Check that a selected module belongs to one of the Course Leader's assigned courses.
        if (moduleId.HasValue)
        {
            var moduleIsAssigned = await _context.Modules.AnyAsync(m =>
                m.ModuleId == moduleId.Value &&
                assignedCourseIds.Contains(m.CourseId));

            if (!moduleIsAssigned)
            {
                return Forbid();
            }

            // If a course was also selected, the module must belong to that course.
            if (courseId.HasValue)
            {
                var moduleMatchesCourse = await _context.Modules.AnyAsync(m =>
                    m.ModuleId == moduleId.Value &&
                    m.CourseId == courseId.Value);

                if (!moduleMatchesCourse)
                {
                    return Forbid();
                }
            }
        }

        // Start with attendance records belonging to the Course Leader's assigned courses.
        // Both the student's course and the module's course are checked.
        var query = _context.Attendances
            .AsNoTracking()
            .Include(a => a.Student)
                .ThenInclude(s => s.Course)
            .Include(a => a.Session)
                .ThenInclude(s => s.ModuleOccurrence)
                    .ThenInclude(o => o.Module)
            .Where(a =>
                assignedCourseIds.Contains(a.Student.CourseId) &&
                assignedCourseIds.Contains(
                    a.Session.ModuleOccurrence.Module.CourseId));

        // Apply the optional start date to the teaching session.
        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;
            query = query.Where(a => a.Session.StartTime >= start);
        }

        // Add one day to the end date so that the whole selected day is included.
        if (endDate.HasValue)
        {
            var endExclusive = endDate.Value.Date.AddDays(1);
            query = query.Where(a => a.Session.StartTime < endExclusive);
        }

        // Apply the selected course filter after checking the user's permissions.
        if (courseId.HasValue)
        {
            query = query.Where(a => a.Student.CourseId == courseId.Value);
        }

        // Apply the selected module filter after checking the user's permissions.
        if (moduleId.HasValue)
        {
            query = query.Where(a =>
                a.Session.ModuleOccurrence.ModuleId == moduleId.Value);
        }

        // Load the permitted attendance records for the report calculations.
        var attendanceRecords = await query.ToListAsync();

        // Prepare the report and retain the selected filter values.
        var model = new AttendanceReportViewModel
        {
            StartDate = startDate,
            EndDate = endDate,
            CourseId = courseId,
            ModuleId = moduleId,

            PresentCount = attendanceRecords.Count(a =>
                a.Status == AttendanceStatus.Present),

            AbsentCount = attendanceRecords.Count(a =>
                a.Status == AttendanceStatus.Absent),

            AuthorisedAbsenceCount = attendanceRecords.Count(a =>
                a.Status == AttendanceStatus.AuthorisedAbsence),

            LateCount = attendanceRecords.Count(a =>
                a.Status == AttendanceStatus.LateArrival),

            LeftEarlyCount = attendanceRecords.Count(a =>
                a.Status == AttendanceStatus.LeftEarly),

            NotRecordedCount = attendanceRecords.Count(a =>
                a.Status == AttendanceStatus.NotRecorded)
        };

        // Calculate the total number of attendance records.
        model.TotalRecords = attendanceRecords.Count;

        // Present, Late Arrival and Left Early count as attended.
        // Authorised Absence and Not Recorded are excluded from the denominator.
        int percentageDenominator =
            model.PresentCount +
            model.AbsentCount +
            model.LateCount +
            model.LeftEarlyCount;

        model.AttendancePercentage = percentageDenominator == 0
            ? 0
            : Math.Round(
                (double)(
                    model.PresentCount +
                    model.LateCount +
                    model.LeftEarlyCount)
                / percentageDenominator * 100,
                1);

        // Group the permitted records by student to create the student-level report.
        model.Students = attendanceRecords
            .GroupBy(a => a.StudentId)
            .Select(group =>
            {
                int attended = group.Count(a =>
                    a.Status == AttendanceStatus.Present ||
                    a.Status == AttendanceStatus.LateArrival ||
                    a.Status == AttendanceStatus.LeftEarly);

                int denominator = group.Count(a =>
                    a.Status == AttendanceStatus.Present ||
                    a.Status == AttendanceStatus.Absent ||
                    a.Status == AttendanceStatus.LateArrival ||
                    a.Status == AttendanceStatus.LeftEarly);

                var student = group.First().Student;

                return new AttendanceReportStudentViewModel
                {
                    StudentId = student.StudentId,
                    StudentName = $"{student.FirstName} {student.LastName}",
                    Email = student.Email,
                    CourseName = student.Course.CourseName,
                    TotalSessions = group.Count(),

                    PresentCount = group.Count(a =>
                        a.Status == AttendanceStatus.Present),

                    AbsentCount = group.Count(a =>
                        a.Status == AttendanceStatus.Absent),

                    AuthorisedAbsenceCount = group.Count(a =>
                        a.Status == AttendanceStatus.AuthorisedAbsence),

                    LateCount = group.Count(a =>
                        a.Status == AttendanceStatus.LateArrival),

                    LeftEarlyCount = group.Count(a =>
                        a.Status == AttendanceStatus.LeftEarly),

                    AttendancePercentage = denominator == 0
                        ? 0
                        : Math.Round(
                            (double)attended / denominator * 100,
                            1)
                };
            })
            .OrderBy(s => s.StudentName)
            .ToList();

        // Populate the course filter using assigned courses only.
        model.Courses = await _context.Courses
            .AsNoTracking()
            .Where(c => assignedCourseIds.Contains(c.CourseId))
            .OrderBy(c => c.CourseName)
            .ToListAsync();

        // Populate the module filter using modules from assigned courses only.
        model.Modules = await _context.Modules
            .AsNoTracking()
            .Where(m => assignedCourseIds.Contains(m.CourseId))
            .OrderBy(m => m.ModuleCode)
            .ToListAsync();

        // Send the completed report to its view.
        return View(model);
    }
}
