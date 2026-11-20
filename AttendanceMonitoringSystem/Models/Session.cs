
namespace AttendanceMonitoringSystem.Models;

public class Session
{
    public int SessionId { get; set; }

    public int ModuleOccurrenceId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string? SessionCode { get; set; }

    public DateTime? CodeExpiresAt { get; set; }

    public string SessionType { get; set; } = string.Empty;

    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

    // Derived from StartTime; not stored as a separate database column.
    public DateTime SessionDate => StartTime.Date;

    public ModuleOccurrence ModuleOccurrence { get; set; } = null!;

    public ICollection<Attendance> Attendances { get; set; }
        = new List<Attendance>();
}
