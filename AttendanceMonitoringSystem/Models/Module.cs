
namespace AttendanceMonitoringSystem.Models;

public class Module
{
    public int ModuleId { get; set; }

    public string ModuleCode { get; set; } = string.Empty;

    public string ModuleName { get; set; } = string.Empty;

    public int CourseId { get; set; }

    public int YearOfStudy { get; set; }

    public Course Course { get; set; } = null!;

    public ICollection<ModuleOccurrence> ModuleOccurrences { get; set; }
        = new List<ModuleOccurrence>();
}
