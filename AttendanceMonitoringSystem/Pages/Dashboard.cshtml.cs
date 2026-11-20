using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Pages;

// UC02 - Record Attendance (wireframes 3.2.2a, 3.2.2c-f; tests T04, T05, T06, T15).
// Follows the workflow in portfolio section 3.4.6.
[Authorize(Roles = RoleNames.Student)]
public class StudentDashboardModel : PageModel
{
    // Codes can be entered from shortly before the session starts until it ends.
    private static readonly TimeSpan EarlyEntryWindow = TimeSpan.FromMinutes(15);

    private readonly ApplicationDbContext _context;
    private readonly AttendanceService _attendanceService;
    private readonly AccessScopeService _scopeService;
    private readonly CodeAttemptLimiter _attemptLimiter;

    public StudentDashboardModel(
        ApplicationDbContext context,
        AttendanceService attendanceService,
        AccessScopeService scopeService,
        CodeAttemptLimiter attemptLimiter)
    {
        _context = context;
        _attendanceService = attendanceService;
        _scopeService = scopeService;
        _attemptLimiter = attemptLimiter;
    }

    public string StudentName { get; private set; } = "Student";
    public string? Message { get; private set; }

    [BindProperty]
    public string? SessionCodeInput { get; set; }

    public string? AttendanceMessage { get; private set; }
    public bool AttendanceMessageIsSuccess { get; private set; }

    public double AttendancePercentage { get; private set; }
    public int AttendedCount { get; private set; }
    public int AbsentCount { get; private set; }

    public List<AttendanceHistoryItem> RecentHistory { get; private set; } = new();

    public async Task OnGetAsync()
    {
        await LoadSummaryAsync();
    }

    public async Task<IActionResult> OnPostSubmitCodeAsync()
    {
        AttendanceMessage = await TryRecordAttendanceAsync();
        await LoadSummaryAsync();
        return Page();
    }

    private async Task<string> TryRecordAttendanceAsync()
    {
        var userId = AccessScopeService.GetUserId(User);
        var student = await _scopeService.GetCurrentStudentAsync(User);

        if (userId == null || student == null)
        {
            return "No student record is linked to your account. Please contact I.T. Support.";
        }

        // T15 - too many invalid codes from this account.
        if (_attemptLimiter.IsLockedOut(userId.Value))
        {
            return "Too many invalid attendance codes have been entered. " +
                   $"Please wait {CodeAttemptLimiter.Window.TotalMinutes:0} minutes and try again, " +
                   "or ask your tutor to record your attendance.";
        }

        var code = (SessionCodeInput ?? "").Trim();

        // Figure 3.2.2c / T04 - empty attendance code.
        if (code.Length == 0)
        {
            return "Please enter an attendance code.";
        }

        var now = DateTime.Now;

        Session? session = null;

        if (code.Length == 6 && code.All(char.IsAsciiDigit))
        {
            session = await _context.Sessions
                .Include(s => s.ModuleOccurrence)
                    .ThenInclude(mo => mo.Module)
                .FirstOrDefaultAsync(s =>
                    s.SessionCode == code &&
                    s.CodeExpiresAt.HasValue &&
                    s.CodeExpiresAt.Value > now &&
                    s.Status != SessionStatus.Cancelled &&
                    s.Status != SessionStatus.Completed &&
                    s.EndTime > now);
        }

        // Session must have started (allowing a short early window).
        if (session != null && session.StartTime - EarlyEntryWindow > now)
        {
            session = null;
        }

        // Figure 3.2.2d / T05 - invalid or expired code.
        if (session == null)
        {
            var remaining = _attemptLimiter.RecordInvalidAttempt(userId.Value);

            return "The attendance code is invalid or has expired. Please check the code and try again." +
                   (remaining is > 0 and <= 2
                       ? $" ({remaining} attempt{(remaining == 1 ? "" : "s")} remaining)"
                       : "");
        }

        // Eligibility - the student must be on the course this module belongs to.
        if (session.ModuleOccurrence.Module.CourseId != student.CourseId)
        {
            _attemptLimiter.RecordInvalidAttempt(userId.Value);
            return "You are not enrolled on the module for this session, so your attendance cannot be recorded.";
        }

        // Figure 3.2.2e / T06 - attendance already recorded.
        var alreadyRecorded = await _context.Attendances.AnyAsync(a =>
            a.StudentId == student.StudentId &&
            a.SessionId == session.SessionId);

        if (alreadyRecorded)
        {
            return "Your attendance for this session has already been recorded.";
        }

        var attendance = new Attendance
        {
            StudentId = student.StudentId,
            SessionId = session.SessionId,
            Status = AttendanceStatus.Present,
            RecordedAt = DateTime.Now,
            RecordedByUserId = userId.Value
        };

        // The unique index on (StudentId, SessionId) also blocks duplicates
        // created by two requests arriving at the same moment.
        if (!_attendanceService.RecordAttendance(attendance))
        {
            return "Your attendance for this session has already been recorded.";
        }

        _attemptLimiter.Reset(userId.Value);

        // Figure 3.2.2f - success.
        AttendanceMessageIsSuccess = true;
        SessionCodeInput = "";
        ModelState.Clear();

        return $"Attendance recorded successfully for {session.ModuleOccurrence.Module.ModuleCode} " +
               $"{session.ModuleOccurrence.Module.ModuleName} ({session.SessionType}, " +
               $"{session.StartTime:dd MMM yyyy HH:mm}).";
    }

    private async Task LoadSummaryAsync()
    {
        StudentName = User.Identity?.Name ?? "Student";

        var student = await _scopeService.GetCurrentStudentAsync(User);

        if (student == null)
        {
            Message = "No student record is linked to your account. Please contact I.T. Support.";
            return;
        }

        var records = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.StudentId == student.StudentId)
            .Select(a => new AttendanceHistoryItem
            {
                SessionDate = a.Session.StartTime,
                ModuleCode = a.Session.ModuleOccurrence.Module.ModuleCode,
                ModuleName = a.Session.ModuleOccurrence.Module.ModuleName,
                SessionType = a.Session.SessionType,
                Status = a.Status
            })
            .ToListAsync();

        AttendancePercentage = AttendanceCalculator.Percentage(records.Select(r => r.Status));
        AttendedCount = records.Count(r => AttendanceCalculator.IsAttended(r.Status));
        AbsentCount = records.Count(r => r.Status == AttendanceStatus.Absent);

        RecentHistory = records
            .OrderByDescending(r => r.SessionDate)
            .Take(5)
            .ToList();
    }
}

public class AttendanceHistoryItem
{
    public DateTime SessionDate { get; set; }
    public string ModuleCode { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public string SessionType { get; set; } = "";
    public AttendanceStatus Status { get; set; }
}
