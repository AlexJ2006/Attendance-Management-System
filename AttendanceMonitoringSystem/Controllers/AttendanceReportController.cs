
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AttendanceMonitoringSystem.Controllers;

[Authorize(Roles = "Lecturer,Adviser,Administrator")]
public class AttendanceReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AttendanceReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        DateTime? startDate,
        DateTime? endDate,
        int? courseId,
        int? moduleId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out int userId))
        {
            return Forbid();
        }

        var query = _context.Attendances
            .AsNoTracking()
            .Include(a => a.Student)
                .ThenInclude(s => s.Course)
            .Include(a => a.Session)
                .ThenInclude(s => s.ModuleOccurrence)
                    .ThenInclude(o => o.Module)
            .AsQueryable();

        // Advisers can only see attendance for their assigned students.
        if (User.IsInRole("Adviser"))
        {
            var assignedStudentIds = await _context.AdviserAssignments
                .Where(a => a.AdviserUserId == userId)
                .Select(a => a.StudentId)
                .ToListAsync();

            query = query.Where(a =>
                assignedStudentIds.Contains(a.StudentId));
        }

        // Apply date filters to the teaching session.
        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;

            query = query.Where(a =>
                a.Session.StartTime >= start);
        }

        if (endDate.HasValue)
        {
            var endExclusive = endDate.Value.Date.AddDays(1);

            query = query.Where(a =>
                a.Session.StartTime < endExclusive);
        }

        if (courseId.HasValue)
        {
            query = query.Where(a =>
                a.Student.CourseId == courseId.Value);
        }

        if (moduleId.HasValue)
        {
            query = query.Where(a =>
                a.Session.ModuleOccurrence.ModuleId == moduleId.Value);
        }

        var attendanceRecords = await query.ToListAsync();

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

        model.TotalRecords = attendanceRecords.Count;

        // Present, late and left early count as attended.
        // Authorised absences and unrecorded entries are excluded
        // from the attendance percentage denominator.
        int percentageDenominator =
            model.PresentCount +
            model.AbsentCount +
            model.LateCount +
            model.LeftEarlyCount;

        model.AttendancePercentage = percentageDenominator == 0
            ? 0
            : Math.Round(
                (double)(model.PresentCount +
                         model.LateCount +
                         model.LeftEarlyCount)
                / percentageDenominator * 100,
                1);

        // Build the student-level report.
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
                        : Math.Round((double)attended / denominator * 100, 1)
                };
            })
            .OrderBy(s => s.StudentName)
            .ToList();

        // Only show courses and modules relevant to the permitted
        // attendance records.
        var relevantCourseIds = attendanceRecords
            .Select(a => a.Student.CourseId)
            .Distinct()
            .ToList();

        var relevantModuleIds = attendanceRecords
            .Select(a => a.Session.ModuleOccurrence.ModuleId)
            .Distinct()
            .ToList();

        model.Courses = await _context.Courses
            .AsNoTracking()
            .Where(c => relevantCourseIds.Contains(c.CourseId))
            .OrderBy(c => c.CourseName)
            .ToListAsync();

        model.Modules = await _context.Modules
            .AsNoTracking()
            .Where(m => relevantModuleIds.Contains(m.ModuleId))
            .OrderBy(m => m.ModuleCode)
            .ToListAsync();

        return View(model);
    }
}
