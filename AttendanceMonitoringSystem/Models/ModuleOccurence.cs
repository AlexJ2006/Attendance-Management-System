namespace AttendanceMonitoringSystem.Models
{
    public class ModuleOccurrence
    {
        public int ModuleOccurrenceId { get; set; }

        public int ModuleId { get; set; }

        public Module Module { get; set; } = null!;

        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}