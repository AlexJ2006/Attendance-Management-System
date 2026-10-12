
namespace AttendanceMonitoringSystem.Models;

public class Session
{
    public int SessionId { get; set; }

    public int ModuleOccurrenceId { get; set; }

    public DateTime SessionDate { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public string SessionType { get; set; } = string.Empty;

    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

    public ModuleOccurrence ModuleOccurrence { get; set; } = null!;

    public ICollection<Attendance> Attendances { get; set; }
        = new List<Attendance>();
}
