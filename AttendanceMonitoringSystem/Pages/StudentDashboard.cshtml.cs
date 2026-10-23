using System.Security.Claims;
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Pages;

[Authorize(Roles = "Student")]
public class StudentDashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public StudentDashboardModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public string StudentName { get; private set; } = "Student";
    public string StudentEmail { get; private set; } = "";
    public string? Message { get; private set; }

    public int AttendedCount { get; private set; }
    public int RecordedSessionCount { get; private set; }
    public decimal AttendancePercentage { get; private set; }

    public List<AttendanceHistoryItem> SessionHistory { get; private set; }
        = new();

    public async Task OnGetAsync()
    {
        StudentName = User.Identity?.Name ?? "Student";
        StudentEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";

        if (string.IsNullOrWhiteSpace(StudentEmail))
        {
            Message = "Your account email could not be found.";
            return;
        }

        // Find the student linked to the authenticated account.
        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Email == StudentEmail);

        if (student == null)
        {
            Message = "No student record is linked to your account.";
            return;
        }

        // Load only this student's attendance records.
        var records = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.StudentId == student.StudentId)
            .Include(a => a.Session)
                .ThenInclude(s => s.ModuleOccurrence)
                    .ThenInclude(mo => mo.Module)
            .ToListAsync();

        var recordedRecords = records
            .Where(a => a.Status != AttendanceStatus.NotRecorded)
            .ToList();

        AttendedCount = recordedRecords.Count(a =>
            a.Status == AttendanceStatus.Present ||
            a.Status == AttendanceStatus.LateArrival ||
            a.Status == AttendanceStatus.LeftEarly);

        RecordedSessionCount = recordedRecords.Count;

        AttendancePercentage = RecordedSessionCount == 0
            ? 0
            : Math.Round(
                (decimal)AttendedCount / RecordedSessionCount * 100,
                1);

        SessionHistory = records
            .OrderByDescending(a => a.Session.StartTime)
            .Select(a => new AttendanceHistoryItem
            {
                SessionDate = a.Session.StartTime,
                ModuleName = a.Session.ModuleOccurrence.Module.ModuleName,
                SessionType = a.Session.SessionType,
                Status = a.Status
            })
            .ToList();
    }
}

public class AttendanceHistoryItem
{
    public DateTime SessionDate { get; set; }
    public string ModuleName { get; set; } = "";
    public string SessionType { get; set; } = "";
    public AttendanceStatus Status { get; set; }
}
