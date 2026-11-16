
using AttendanceMonitoringSystem.Models;

namespace AttendanceMonitoringSystem.Models
{
    // Holds the information displayed when a Course Leader opens a module.
    public class CourseLeaderModuleViewModel
    {
        // The module being monitored.
        public Module Module { get; set; } = null!;

        // The overall attendance percentage for this module.
        public double AttendancePercentage { get; set; }

        // Student attendance summaries for this module.
        public List<CourseLeaderStudentSummaryViewModel> Students { get; set; } = new();
    }
}
