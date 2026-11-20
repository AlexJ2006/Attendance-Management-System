
namespace AttendanceMonitoringSystem.Models;

public class ModuleOccurrence
{
    public int ModuleOccurrenceId { get; set; }

    public int ModuleId { get; set; }

    public string AcademicYear { get; set; } = string.Empty;

    public string Semester { get; set; } = string.Empty;

    public Module Module { get; set; } = null!;

    public ICollection<Session> Sessions { get; set; }
        = new List<Session>();
}
