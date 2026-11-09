
using System.Security.Claims;
using System.Security.Cryptography;
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Pages;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DashboardModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public string UserName { get; private set; } = "User";

    public List<string> Roles { get; private set; } = new();

    public List<SessionOption> AvailableSessions { get; private set; } = new();

    public List<SessionOption> RegisterSessions { get; private set; } = new();

    public string? GeneratedCode { get; private set; }

    public DateTime? CodeExpiresAt { get; private set; }

    public string? Message { get; private set; }

    public class SessionOption
    {
        public int SessionId { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        LoadIdentity();
        await LoadAvailableSessionsAsync();
        await LoadRegisterSessionsAsync();
    }

    public async Task<IActionResult> OnPostGenerateCodeAsync(int sessionId)
    {
        LoadIdentity();

        if (!Roles.Contains("Lecturer"))
        {
            return Forbid();
        }

        await LoadAvailableSessionsAsync();
        await LoadRegisterSessionsAsync();

        var session = await _context.Sessions
            .Include(s => s.ModuleOccurrence)
            .ThenInclude(mo => mo.Module)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
        {
            Message = "The selected teaching session could not be found.";
            return Page();
        }

        if (session.Status == SessionStatus.Cancelled ||
            session.Status == SessionStatus.Completed ||
            session.EndTime <= DateTime.Now)
        {
            Message = "A code cannot be generated for a cancelled, completed, or ended session.";
            return Page();
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D6");

        var expiry = DateTime.Now.AddMinutes(10);

        session.SessionCode = code;
        session.CodeExpiresAt = expiry;

        await _context.SaveChangesAsync();

        GeneratedCode = code;
        CodeExpiresAt = expiry;
        Message = $"Code generated for {session.ModuleOccurrence.Module.ModuleCode}.";

        return Page();
    }

    private void LoadIdentity()
    {
        UserName = User.Identity?.Name ?? "User";

        Roles = User.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct()
            .ToList();
    }

    private async Task LoadRegisterSessionsAsync()
    {
        RegisterSessions = await _context.Sessions
            .AsNoTracking()
            .Include(s => s.ModuleOccurrence)
                .ThenInclude(mo => mo.Module)
            .OrderByDescending(s => s.StartTime)
            .Select(s => new SessionOption
            {
                SessionId = s.SessionId,
                Description = s.ModuleOccurrence.Module.ModuleCode
                    + " — " + s.ModuleOccurrence.Module.ModuleName
                    + " (" + s.StartTime.ToString() + ")"
                    + " — " + s.Status.ToString()
            })
            .ToListAsync();
    }

    private async Task LoadAvailableSessionsAsync()
    {
        AvailableSessions = await _context.Sessions
            .AsNoTracking()
            .Include(s => s.ModuleOccurrence)
            .ThenInclude(mo => mo.Module)
            .Where(s =>
                s.Status != SessionStatus.Cancelled &&
                s.Status != SessionStatus.Completed &&
                s.EndTime > DateTime.Now)
            .OrderBy(s => s.StartTime)
            .Select(s => new SessionOption
            {
                SessionId = s.SessionId,
                Description = s.ModuleOccurrence.Module.ModuleCode
                    + " — " + s.ModuleOccurrence.Module.ModuleName
                    + " (" + s.StartTime.ToString() + ")"
            })
            .ToListAsync();
    }
}
