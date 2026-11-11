
using System.Security.Claims;
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Pages;

[Authorize(Roles = "Student")]
public class StudentDashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly AttendanceService _attendanceService;

    public StudentDashboardModel(
        ApplicationDbContext context,
        AttendanceService attendanceService)
    {
        _context = context;
        _attendanceService = attendanceService;
    }

    public string StudentName { get; private set; } = "Student";
    public string StudentEmail { get; private set; } = "";
    public string? Message { get; private set; }

    [BindProperty]
    public string SessionCodeInput { get; set; } = "";

    public string? AttendanceMessage { get; private set; }
    public bool AttendanceMessageIsSuccess { get; private set; }

    public int AttendedCount { get; private set; }
    public int RecordedSessionCount { get; private set; }
    public int EligibleSessionCount { get; private set; }

    public int PresentCount { get; private set; }
    public int AbsentCount { get; private set; }
    public int LateCount { get; private set; }
    public int AuthorisedAbsenceCount { get; private set; }
    public int LeftEarlyCount { get; private set; }

    public decimal AttendancePercentage { get; private set; }

    public List<AttendanceHistoryItem> SessionHistory { get; private set; }
        = new();

    public async Task OnGetAsync()
    {
        await LoadDashboardAsync();
    }

    public async Task<IActionResult> OnPostSubmitCodeAsync()
    {
        string code = (SessionCodeInput ?? "").Trim();

        if (code.Length != 6 || !code.All(char.IsAsciiDigit))
        {
            AttendanceMessage = "Enter a valid six-digit session code.";
            await LoadDashboardAsync();
            return Page();
        }

        string email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        string userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        if (string.IsNullOrWhiteSpace(email) ||
            !int.TryParse(userIdValue, out int userId))
        {
            AttendanceMessage = "Your account could not be verified. Please sign in again.";
            await LoadDashboardAsync();
            return Page();
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Email == email);

        if (student == null)
        {
            AttendanceMessage = "No student record is linked to your account.";
            await LoadDashboardAsync();
            return Page();
        }

        DateTime now = DateTime.Now;

        var session = await _context.Sessions
            .FirstOrDefaultAsync(s =>
                s.SessionCode == code &&
                s.CodeExpiresAt.HasValue &&
                s.CodeExpiresAt.Value > now &&
                s.StartTime <= now &&
                s.EndTime > now &&
                s.Status == SessionStatus.InProgress);

        if (session == null)
        {
            AttendanceMessage =
                "This code is invalid, expired, or the session is not currently in progress.";

            await LoadDashboardAsync();
            return Page();
        }

        bool alreadyRecorded = await _context.Attendances
            .AnyAsync(a =>
                a.StudentId == student.StudentId &&
                a.SessionId == session.SessionId);

        if (alreadyRecorded)
        {
            AttendanceMessage =
                "Your attendance for this session has already been recorded.";

            await LoadDashboardAsync();
            return Page();
        }

        var attendance = new Attendance
        {
            StudentId = student.StudentId,
            SessionId = session.SessionId,
            Status = AttendanceStatus.Present,
            RecordedAt = DateTime.UtcNow,
            RecordedByUserId = userId
        };

        if (!_attendanceService.RecordAttendance(attendance))
        {
            AttendanceMessage =
                "Attendance could not be recorded. It may already exist; please refresh and check your attendance history.";

            await LoadDashboardAsync();
            return Page();
        }

        AttendanceMessage = "Attendance recorded successfully!";
        AttendanceMessageIsSuccess = true;
        SessionCodeInput = "";

        await LoadDashboardAsync();
        return Page();
    }

    private async Task LoadDashboardAsync()
    {
        StudentName = User.Identity?.Name ?? "Student";
        StudentEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";

        if (string.IsNullOrWhiteSpace(StudentEmail))
        {
            Message = "Your account email could not be found.";
            return;
        }

        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Email == StudentEmail);

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

        var recordedRecords = records
            .Where(a => a.Status != AttendanceStatus.NotRecorded)
            .ToList();

        PresentCount = recordedRecords.Count(a =>
            a.Status == AttendanceStatus.Present);

        AbsentCount = recordedRecords.Count(a =>
            a.Status == AttendanceStatus.Absent);

        LateCount = recordedRecords.Count(a =>
            a.Status == AttendanceStatus.LateArrival);

        AuthorisedAbsenceCount = recordedRecords.Count(a =>
            a.Status == AttendanceStatus.AuthorisedAbsence);

        LeftEarlyCount = recordedRecords.Count(a =>
            a.Status == AttendanceStatus.LeftEarly);

        AttendedCount = PresentCount + LateCount + LeftEarlyCount;

        RecordedSessionCount = recordedRecords.Count;

        // Authorised absences are excluded from the attendance percentage.
        EligibleSessionCount =
            PresentCount + AbsentCount + LateCount + LeftEarlyCount;

        AttendancePercentage = EligibleSessionCount == 0
            ? 0
            : Math.Round(
                (decimal)AttendedCount / EligibleSessionCount * 100,
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
