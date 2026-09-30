namespace AttendanceMonitoringSystem.Models
{
    public class Module
    {
        public int ModuleId { get; set; }

        public int CourseId { get; set; }

        public string ModuleName { get; set; } = string.Empty;

        public Course Course { get; set; } = null!;

        public ICollection<ModuleOccurrence> ModuleOccurrences { get; set; } = new List<ModuleOccurrence>();
    }
}