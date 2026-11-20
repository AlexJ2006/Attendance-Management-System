
namespace AttendanceMonitoringSystem.Models
{
    public class ModuleLeaderAssignment
    {
        public int ModuleLeaderAssignmentId { get; set; }

        public int ModuleLeaderUserId { get; set; }

        public int ModuleId { get; set; }

        public User ModuleLeader { get; set; } = null!;

        public Module Module { get; set; } = null!;
    }
}
