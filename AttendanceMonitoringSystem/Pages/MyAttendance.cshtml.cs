using System.Security.Claims;
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Pages;

[Authorize(Roles = "Student")]
public class MyAttendanceModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public MyAttendanceModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public string Message { get; private set; } = "";
    public int AttendedCount { get; private set; }
    public int RecordedSessionCount { get; private set; }
    public decimal AttendancePercentage { get; private set; }

    public List<AttendanceHistoryItem> SessionHistory { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(email))
        {
            Message = "Your account email could not be found. Please sign in again.";
            return;
        }

        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Email == email);

        if (student == null)
        {
            Message = "No student record is linked to your account.";
            return;
        }

        var records = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.StudentId == student.StudentId)
            .Include(a => a.Session)
                .ThenInclude(s => s.ModuleOccurrence)
                    .ThenInclude(mo => mo.Module)
            .ToListAsync();

        var recorded = records
            .Where(a => a.Status != AttendanceStatus.NotRecorded)
            .ToList();

        // Count sessions where the student attended, including late arrivals
        // and sessions where the student left early.
        AttendedCount = recorded.Count(a =>
            a.Status == AttendanceStatus.Present ||
            a.Status == AttendanceStatus.LateArrival ||
            a.Status == AttendanceStatus.LeftEarly);

        // Keep the total recorded session count for the attendance history.
        RecordedSessionCount = recorded.Count;

        // Authorised absences must not count against the student's attendance.
        // Not Recorded sessions have already been excluded above.
        var eligibleSessionCount = recorded.Count(a =>
            a.Status == AttendanceStatus.Present ||
            a.Status == AttendanceStatus.Absent ||
            a.Status == AttendanceStatus.LateArrival ||
            a.Status == AttendanceStatus.LeftEarly);

        // Calculate attendance using only eligible sessions.
        AttendancePercentage = eligibleSessionCount == 0
            ? 0
            : Math.Round(
                (decimal)AttendedCount / eligibleSessionCount * 100,
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
