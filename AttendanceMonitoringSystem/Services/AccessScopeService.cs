using System.Security.Claims;
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Services;

// The academic scope of a signed-in member of staff (ADR05).
// A user may hold several roles, so the scope is the union of
// everything each of their roles allows.
public class AccessScope
{
    // Administrators can see every record.
    public bool All { get; init; }

    // Module occurrences whose sessions and attendance the user may see.
    public HashSet<int> OccurrenceIds { get; init; } = new();

    // Individual students the user may see (Academic Advisers).
    public HashSet<int> StudentIds { get; init; } = new();

    // Module occurrences the user may record or correct attendance for.
    // Only Tutors assigned to the occurrence (and Administrators) may edit.
    public HashSet<int> EditableOccurrenceIds { get; init; } = new();

    public bool IsEmpty =>
        !All && OccurrenceIds.Count == 0 && StudentIds.Count == 0;

    public bool CanView(int occurrenceId, int studentId) =>
        All || OccurrenceIds.Contains(occurrenceId) ||
        StudentIds.Contains(studentId);

    public bool CanEdit(int occurrenceId) =>
        All || EditableOccurrenceIds.Contains(occurrenceId);
}

// Works out what the signed-in user is allowed to access.
// Every check uses the identity in the authentication cookie, never a
// value from the URL, so changing a URL cannot widen access (T07, T08).
public class AccessScopeService
{
    private readonly ApplicationDbContext _context;

    public AccessScopeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public static int? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }

    // Returns the student record linked to the signed-in account.
    public async Task<Student?> GetCurrentStudentAsync(ClaimsPrincipal user)
    {
        var userId = GetUserId(user);

        if (userId == null)
        {
            return null;
        }

        return await _context.Students
            .AsNoTracking()
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.UserId == userId.Value);
    }

    public async Task<AccessScope> GetStaffScopeAsync(ClaimsPrincipal user)
    {
        var userId = GetUserId(user);

        if (userId == null)
        {
            return new AccessScope();
        }

        if (user.IsInRole(RoleNames.Administrator))
        {
            return new AccessScope { All = true };
        }

        var occurrenceIds = new HashSet<int>();
        var editable = new HashSet<int>();
        var studentIds = new HashSet<int>();

        if (user.IsInRole(RoleNames.Tutor))
        {
            var tutorOccurrences = await _context.TutorAssignments
                .Where(a => a.TutorUserId == userId.Value)
                .Select(a => a.ModuleOccurrenceId)
                .ToListAsync();

            occurrenceIds.UnionWith(tutorOccurrences);
            editable.UnionWith(tutorOccurrences);
        }

        if (user.IsInRole(RoleNames.ModuleLeader))
        {
            occurrenceIds.UnionWith(await _context.ModuleOccurrences
                .Where(mo => _context.ModuleLeaderAssignments.Any(a =>
                    a.ModuleLeaderUserId == userId.Value &&
                    a.ModuleId == mo.ModuleId))
                .Select(mo => mo.ModuleOccurrenceId)
                .ToListAsync());
        }

        if (user.IsInRole(RoleNames.CourseLeader))
        {
            occurrenceIds.UnionWith(await _context.ModuleOccurrences
                .Where(mo => _context.CourseLeaderAssignments.Any(a =>
                    a.CourseLeaderUserId == userId.Value &&
                    a.CourseId == mo.Module.CourseId))
                .Select(mo => mo.ModuleOccurrenceId)
                .ToListAsync());
        }

        if (user.IsInRole(RoleNames.Adviser))
        {
            studentIds.UnionWith(await _context.AdviserAssignments
                .Where(a => a.AdviserUserId == userId.Value)
                .Select(a => a.StudentId)
                .ToListAsync());
        }

        return new AccessScope
        {
            OccurrenceIds = occurrenceIds,
            EditableOccurrenceIds = editable,
            StudentIds = studentIds
        };
    }

    // Restricts an attendance query to the records inside the user's scope.
    public static IQueryable<Attendance> ApplyScope(
        IQueryable<Attendance> query,
        AccessScope scope)
    {
        if (scope.All)
        {
            return query;
        }

        var occurrenceIds = scope.OccurrenceIds.ToList();
        var studentIds = scope.StudentIds.ToList();

        return query.Where(a =>
            occurrenceIds.Contains(a.Session.ModuleOccurrenceId) ||
            studentIds.Contains(a.StudentId));
    }
}
