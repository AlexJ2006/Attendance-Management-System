
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AttendanceMonitoringSystem.Controllers;

// Only users with the existing Lecturer role can access Tutor session functions.
[Authorize(Roles = "Lecturer")]
public class SessionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AttendanceService _attendanceService;

    public SessionsController(
        ApplicationDbContext context,
        AttendanceService attendanceService)
    {
        _context = context;
        _attendanceService = attendanceService;
    }

    // Get the logged-in Lecturer's database user ID.
    private int? GetCurrentLecturerUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(userIdValue, out int userId) && userId > 0)
        {
            return userId;
        }

        return null;
    }

    // Check whether the logged-in Lecturer is assigned to an occurrence.
    private async Task<bool> IsAssignedToOccurrenceAsync(int occurrenceId)
    {
        var userId = GetCurrentLecturerUserId();

        if (userId == null)
        {
            return false;
        }

        return await _context.TutorAssignments.AnyAsync(a =>
            a.TutorUserId == userId.Value &&
            a.ModuleOccurrenceId == occurrenceId);
    }

    // CREATE SESSION

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateSessionViewModel();

        // Only show module occurrences assigned to this Tutor.
        await PopulateModuleOccurrencesAsync(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSessionViewModel model)
    {
        var now = DateTime.Now;

        if (model.StartTime == default)
        {
            ModelState.AddModelError(
                nameof(model.StartTime),
                "Please enter a valid start date and time.");
        }

        if (model.EndTime == default)
        {
            ModelState.AddModelError(
                nameof(model.EndTime),
                "Please enter a valid end date and time.");
        }

        if (model.EndTime <= model.StartTime)
        {
            ModelState.AddModelError(
                nameof(model.EndTime),
                "The end time must be after the start time.");
        }

        if (model.EndTime <= now)
        {
            ModelState.AddModelError(
                nameof(model.EndTime),
                "The session must have an end time in the future.");
        }

        var allowedTypes = new[]
        {
            "Lecture", "Seminar", "Tutorial", "Practical"
        };

        if (!allowedTypes.Contains(model.SessionType))
        {
            ModelState.AddModelError(
                nameof(model.SessionType),
                "Please select a valid session type.");
        }

        var occurrence = await _context.ModuleOccurrences
            .Include(mo => mo.Module)
            .FirstOrDefaultAsync(mo =>
                mo.ModuleOccurrenceId == model.ModuleOccurrenceId);

        if (occurrence == null)
        {
            ModelState.AddModelError(
                nameof(model.ModuleOccurrenceId),
                "Please select a valid module occurrence.");
        }
        else if (!await IsAssignedToOccurrenceAsync(
                     model.ModuleOccurrenceId))
        {
            // Prevent a Tutor creating sessions for an unassigned occurrence.
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await PopulateModuleOccurrencesAsync(model);
            return View(model);
        }

        var session = new Session
        {
            ModuleOccurrenceId = model.ModuleOccurrenceId,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            SessionType = model.SessionType,
            Status = model.StartTime <= now
                ? SessionStatus.InProgress
                : SessionStatus.Scheduled,
            SessionCode = null,
            CodeExpiresAt = null
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"The {session.SessionType.ToLower()} session for " +
            $"{occurrence!.Module.ModuleCode} was created successfully.";

        return RedirectToAction(nameof(Create));
    }

    // Populate the occurrence dropdown using only this Tutor's assignments.
    private async Task PopulateModuleOccurrencesAsync(
        CreateSessionViewModel model)
    {
        var userId = GetCurrentLecturerUserId();

        if (userId == null)
        {
            model.ModuleOccurrences = new List<SelectListItem>();
            return;
        }

        model.ModuleOccurrences = await _context.ModuleOccurrences
            .AsNoTracking()
            .Where(mo => _context.TutorAssignments.Any(a =>
                a.TutorUserId == userId.Value &&
                a.ModuleOccurrenceId == mo.ModuleOccurrenceId))
            .Include(mo => mo.Module)
            .OrderBy(mo => mo.Module.ModuleCode)
            .Select(mo => new SelectListItem
            {
                Value = mo.ModuleOccurrenceId.ToString(),
                Text = mo.Module.ModuleCode + " — " +
                       mo.Module.ModuleName + " (" +
                       mo.AcademicYear + ", " + mo.Semester + ")"
            })
            .ToListAsync();
    }

    // LECTURER ATTENDANCE REGISTER

    [HttpGet]
    public async Task<IActionResult> Register(int id)
    {
        var session = await _context.Sessions
            .AsNoTracking()
            .Include(s => s.ModuleOccurrence)
                .ThenInclude(mo => mo.Module)
            .FirstOrDefaultAsync(s => s.SessionId == id);

        if (session == null)
        {
            return NotFound();
        }

        // Check the Tutor assignment before displaying the register.
        if (!await IsAssignedToOccurrenceAsync(
                session.ModuleOccurrenceId))
        {
            return Forbid();
        }

        var module = session.ModuleOccurrence.Module;

        var students = await _context.Students
            .AsNoTracking()
            .Where(s => s.CourseId == module.CourseId)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();

        var attendanceRecords = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.SessionId == id)
            .ToDictionaryAsync(a => a.StudentId);

        var model = new LecturerAttendanceRegisterViewModel
        {
            SessionId = session.SessionId,
            ModuleCode = module.ModuleCode,
            ModuleName = module.ModuleName,
            SessionType = session.SessionType,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            SessionStatus = session.Status,

            Students = students.Select(student =>
            {
                attendanceRecords.TryGetValue(
                    student.StudentId, out var attendance);

                return new LecturerAttendanceStudentViewModel
                {
                    StudentId = student.StudentId,
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    Email = student.Email,
                    AttendanceId = attendance?.AttendanceId,
                    Status = attendance?.Status
                };
            }).ToList()
        };

        model.PresentCount = model.Students.Count(s =>
            s.Status == AttendanceStatus.Present);

        model.AbsentCount = model.Students.Count(s =>
            s.Status == AttendanceStatus.Absent);

        model.NotRecordedCount = model.Students.Count(s =>
            s.Status == null ||
            s.Status == AttendanceStatus.NotRecorded);

        return View(model);
    }

    // MANUAL ATTENDANCE RECORDING AND CORRECTIONS

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordManualAttendance(
        int sessionId,
        int studentId,
        AttendanceStatus status,
        string? reason)
    {
        var allowedStatuses = new[]
        {
            AttendanceStatus.Present,
            AttendanceStatus.Absent,
            AttendanceStatus.AuthorisedAbsence,
            AttendanceStatus.LateArrival,
            AttendanceStatus.LeftEarly
        };

        if (!allowedStatuses.Contains(status))
        {
            TempData["ErrorMessage"] =
                "Please select a valid attendance status.";

            return RedirectToAction(nameof(Register), new { id = sessionId });
        }

        var lecturerUserId = GetCurrentLecturerUserId();

        if (lecturerUserId == null)
        {
            return Forbid();
        }

        var session = await _context.Sessions
            .Include(s => s.ModuleOccurrence)
                .ThenInclude(mo => mo.Module)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
        {
            return NotFound();
        }

        // Check assignment before allowing any attendance changes.
        if (!await IsAssignedToOccurrenceAsync(
                session.ModuleOccurrenceId))
        {
            return Forbid();
        }

        if (session.Status == SessionStatus.Cancelled)
        {
            TempData["ErrorMessage"] =
                "Attendance cannot be changed for a cancelled session.";

            return RedirectToAction(nameof(Register), new { id = sessionId });
        }

        var courseId = session.ModuleOccurrence.Module.CourseId;

        var studentExists = await _context.Students.AnyAsync(s =>
            s.StudentId == studentId &&
            s.CourseId == courseId);

        if (!studentExists)
        {
            TempData["ErrorMessage"] =
                "The selected student does not belong to this session's course.";

            return RedirectToAction(nameof(Register), new { id = sessionId });
        }

        var existingAttendance = await _context.Attendances
            .FirstOrDefaultAsync(a =>
                a.StudentId == studentId &&
                a.SessionId == sessionId);

        if (existingAttendance != null)
        {
            if (existingAttendance.Status == status)
            {
                TempData["ErrorMessage"] =
                    "That student already has the selected attendance status.";

                return RedirectToAction(nameof(Register), new { id = sessionId });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] =
                    "Please provide a reason when changing an existing attendance record.";

                return RedirectToAction(nameof(Register), new { id = sessionId });
            }

            var corrected = _attendanceService.CorrectAttendance(
                existingAttendance.AttendanceId,
                status,
                lecturerUserId.Value,
                reason);

            TempData[corrected ? "SuccessMessage" : "ErrorMessage"] =
                corrected
                    ? "The attendance status was updated and the correction was recorded."
                    : "The attendance status could not be updated.";

            return RedirectToAction(nameof(Register), new { id = sessionId });
        }

        var lecturerExists = await _context.Users.AnyAsync(u =>
            u.UserId == lecturerUserId.Value);

        if (!lecturerExists)
        {
            return Forbid();
        }

        var attendance = new Attendance
        {
            StudentId = studentId,
            SessionId = sessionId,
            Status = status,
            RecordedAt = DateTime.UtcNow,
            RecordedByUserId = lecturerUserId.Value
        };

        _context.Attendances.Add(attendance);

        try
        {
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Attendance was recorded successfully.";
        }
        catch (DbUpdateException)
        {
            _context.Entry(attendance).State = EntityState.Detached;

            TempData["ErrorMessage"] =
                "The attendance record could not be saved. It may already exist.";
        }

        return RedirectToAction(nameof(Register), new { id = sessionId });
    }

    // GENERATE SESSION CODE

    [HttpGet]
    public async Task<IActionResult> GenerateCode()
    {
        // Only sessions belonging to assigned occurrences are displayed.
        await PopulateAvailableSessionsAsync();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateCode(int sessionId)
    {
        await PopulateAvailableSessionsAsync();

        var session = await _context.Sessions
            .Include(s => s.ModuleOccurrence)
                .ThenInclude(mo => mo.Module)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
        {
            ModelState.AddModelError(
                "",
                "Please select a valid teaching session.");

            return View();
        }

        // Prevent generating a code for another Tutor's session.
        if (!await IsAssignedToOccurrenceAsync(
                session.ModuleOccurrenceId))
        {
            return Forbid();
        }

        if (session.Status == SessionStatus.Cancelled ||
            session.Status == SessionStatus.Completed ||
            session.EndTime <= DateTime.Now)
        {
            ModelState.AddModelError(
                "",
                "A code cannot be generated for a cancelled, completed, or ended session.");

            return View();
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D6");

        var expiry = DateTime.Now.AddMinutes(10);

        session.SessionCode = code;
        session.CodeExpiresAt = expiry;

        await _context.SaveChangesAsync();

        TempData["GeneratedCode"] = code;
        TempData["CodeExpiresAt"] =
            expiry.ToString("dd MMM yyyy HH:mm:ss");

        TempData["CodeMessage"] =
            $"A code has been generated for {session.ModuleOccurrence.Module.ModuleCode}.";

        return RedirectToAction(nameof(GenerateCode));
    }

    // Populate the session dropdown using only assigned module occurrences.
    private async Task PopulateAvailableSessionsAsync()
    {
        var userId = GetCurrentLecturerUserId();

        if (userId == null)
        {
            ViewBag.AvailableSessions = new List<SelectListItem>();
            return;
        }

        ViewBag.AvailableSessions = await _context.Sessions
            .AsNoTracking()
            .Include(s => s.ModuleOccurrence)
                .ThenInclude(mo => mo.Module)
            .Where(s =>
                s.Status != SessionStatus.Cancelled &&
                s.Status != SessionStatus.Completed &&
                s.EndTime > DateTime.Now &&
                _context.TutorAssignments.Any(a =>
                    a.TutorUserId == userId.Value &&
                    a.ModuleOccurrenceId == s.ModuleOccurrenceId))
            .OrderBy(s => s.StartTime)
            .Select(s => new SelectListItem
            {
                Value = s.SessionId.ToString(),
                Text = s.ModuleOccurrence.Module.ModuleCode
                    + " — " + s.ModuleOccurrence.Module.ModuleName
                    + " (" + s.StartTime.ToString() + ")"
            })
            .ToListAsync();
    }
}
