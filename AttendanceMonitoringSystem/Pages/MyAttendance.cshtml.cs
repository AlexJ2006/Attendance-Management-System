using System.Globalization;
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Pages;

// UC03 - View Personal Attendance Statistics (wireframe 3.2.2b, test T07).
// The student is identified only from the sign-in cookie (section 3.4.7);
// this page takes no ID parameter, so changing the URL cannot reveal
// another student's records.
[Authorize(Roles = RoleNames.Student)]
public class MyAttendanceModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly AccessScopeService _scopeService;

    public MyAttendanceModel(ApplicationDbContext context, AccessScopeService scopeService)
    {
        _context = context;
        _scopeService = scopeService;
    }

    public string? Message { get; private set; }
    public double OverallPercentage { get; private set; }
    public int AttendedCount { get; private set; }
    public int AbsentCount { get; private set; }
    public int AuthorisedCount { get; private set; }

    public List<ModuleAttendanceSummary> Modules { get; private set; } = new();
    public List<MonthlyAttendance> Months { get; private set; } = new();
    public List<AttendanceHistoryItem> History { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var student = await _scopeService.GetCurrentStudentAsync(User);

        if (student == null)
        {
            Message = "No student record is linked to your account. Please contact I.T. Support.";
            return;
        }

        try
        {
            History = await _context.Attendances
                .AsNoTracking()
                .Where(a => a.StudentId == student.StudentId)
                .OrderByDescending(a => a.Session.StartTime)
                .Select(a => new AttendanceHistoryItem
                {
                    SessionDate = a.Session.StartTime,
                    ModuleCode = a.Session.ModuleOccurrence.Module.ModuleCode,
                    ModuleName = a.Session.ModuleOccurrence.Module.ModuleName,
                    SessionType = a.Session.SessionType,
                    Status = a.Status
                })
                .ToListAsync();
        }
        catch (Exception)
        {
            // UC03 alternative scenario - data retrieval fails.
            Message = "Your attendance information could not be loaded at the moment. Please try again later.";
            return;
        }

        if (History.Count == 0)
        {
            // UC03 alternative scenario - no records.
            Message = "No attendance records are available for you yet.";
        }

        var statuses = History.Select(h => h.Status).ToList();
        OverallPercentage = AttendanceCalculator.Percentage(statuses);
        AttendedCount = statuses.Count(AttendanceCalculator.IsAttended);
        AbsentCount = statuses.Count(s => s == AttendanceStatus.Absent);
        AuthorisedCount = statuses.Count(s => s == AttendanceStatus.AuthorisedAbsence);

        // Every module on the student's course is shown, even before any
        // attendance has been recorded for it.
        var courseModules = await _context.Modules
            .AsNoTracking()
            .Where(m => m.CourseId == student.CourseId)
            .OrderBy(m => m.ModuleCode)
            .ToListAsync();

        Modules = courseModules.Select(m =>
        {
            var moduleStatuses = History
                .Where(h => h.ModuleCode == m.ModuleCode)
                .Select(h => h.Status)
                .ToList();

            return new ModuleAttendanceSummary
            {
                ModuleCode = m.ModuleCode,
                ModuleName = m.ModuleName,
                Sessions = moduleStatuses.Count,
                Percentage = AttendanceCalculator.Percentage(moduleStatuses)
            };
        }).ToList();

        Months = History
            .GroupBy(h => new DateTime(h.SessionDate.Year, h.SessionDate.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new MonthlyAttendance
            {
                Label = g.Key.ToString("MMM yyyy", CultureInfo.GetCultureInfo("en-GB")),
                Sessions = g.Count(),
                Percentage = AttendanceCalculator.Percentage(g.Select(h => h.Status))
            })
            .ToList();
    }
}

public class ModuleAttendanceSummary
{
    public string ModuleCode { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public int Sessions { get; set; }
    public double Percentage { get; set; }
}

public class MonthlyAttendance
{
    public string Label { get; set; } = "";
    public int Sessions { get; set; }
    public double Percentage { get; set; }
}
