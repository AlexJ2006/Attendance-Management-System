using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Controllers;

// Staff Attendance Dashboard (wireframe 3.2.4) and UC04 - Manage a session
// register. A Tutor only sees the module occurrences they are assigned to.
[Authorize(Roles = RoleNames.Tutor)]
public class TutorController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TutorController> _logger;

    public TutorController(ApplicationDbContext context, ILogger<TutorController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? occurrenceId)
    {
        var userId = AccessScopeService.GetUserId(User);

        if (userId == null)
        {
            return Forbid();
        }

        var model = new TutorDashboardViewModel();

        try
        {
            model.Occurrences = await _context.ModuleOccurrences
                .AsNoTracking()
                .Include(mo => mo.Module)
                .Where(mo => _context.TutorAssignments.Any(a =>
                    a.TutorUserId == userId.Value &&
                    a.ModuleOccurrenceId == mo.ModuleOccurrenceId))
                .OrderBy(mo => mo.Module.ModuleCode)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tutor dashboard failed to load.");

            // Figure 3.2.4d - register load failure.
            model.ErrorMessage = "The attendance register could not be loaded. Please try again.";
            return View(model);
        }

        if (model.Occurrences.Count == 0)
        {
            // Figure 3.2.4b - no enrolment / assignment details found.
            model.InfoMessage = "You are not assigned to any modules yet. Please contact your Administrator.";
            return View(model);
        }

        // Default to the first assigned module.
        occurrenceId ??= model.Occurrences[0].ModuleOccurrenceId;

        // Refuse occurrences that belong to another Tutor (test T08).
        var selected = model.Occurrences
            .FirstOrDefault(o => o.ModuleOccurrenceId == occurrenceId);

        if (selected == null)
        {
            return Forbid();
        }

        model.Selected = selected;

        var sessions = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.ModuleOccurrenceId == selected.ModuleOccurrenceId)
            .OrderByDescending(s => s.StartTime)
            .Select(s => new TutorSessionSummary
            {
                SessionId = s.SessionId,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                SessionType = s.SessionType,
                Status = s.Status,
                HasLiveCode = s.SessionCode != null && s.CodeExpiresAt > DateTime.Now,
                Statuses = s.Attendances.Select(a => a.Status).ToList()
            })
            .ToListAsync();

        var enrolled = await _context.Students
            .CountAsync(s => s.CourseId == selected.Module.CourseId);

        foreach (var session in sessions)
        {
            session.EnrolledCount = enrolled;
        }

        model.Sessions = sessions;
        model.EnrolledCount = enrolled;
        model.ModuleAttendanceRate = AttendanceCalculator.Percentage(
            sessions.SelectMany(s => s.Statuses));

        if (sessions.Count == 0)
        {
            // Figure 3.2.4c - no register available.
            model.InfoMessage = "No register is available for this module yet. Create a session to start taking attendance.";
        }

        return View(model);
    }
}

public class TutorDashboardViewModel
{
    public List<ModuleOccurrence> Occurrences { get; set; } = new();
    public ModuleOccurrence? Selected { get; set; }
    public List<TutorSessionSummary> Sessions { get; set; } = new();
    public int EnrolledCount { get; set; }
    public double ModuleAttendanceRate { get; set; }
    public string? InfoMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class TutorSessionSummary
{
    public int SessionId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string SessionType { get; set; } = "";
    public SessionStatus Status { get; set; }
    public bool HasLiveCode { get; set; }
    public int EnrolledCount { get; set; }
    public List<AttendanceStatus> Statuses { get; set; } = new();

    public int AttendedCount => Statuses.Count(AttendanceCalculator.IsAttended);
    public double Percentage => AttendanceCalculator.Percentage(Statuses);
}
