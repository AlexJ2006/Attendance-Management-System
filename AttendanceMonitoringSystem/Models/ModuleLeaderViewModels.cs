
using System.Collections.Generic;

namespace AttendanceMonitoringSystem.Models
{
    // Holds the information displayed on the Module Leader dashboard.
    public class ModuleLeaderDashboardViewModel
    {
        // Minimum attendance percentage required for compliance.
        public int AttendanceWarningThreshold { get; set; } = 75;

        // Modules assigned to the signed-in Module Leader.
        public List<ModuleLeaderModuleSummaryViewModel> Modules { get; set; }
            = new();

        // Students with attendance below the 75% threshold.
        public List<ModuleLeaderStudentSummaryViewModel> LowAttendanceStudents
            { get; set; } = new();

        // Students with two or more consecutive weeks without engagement.
        public List<ModuleLeaderStudentSummaryViewModel> DisengagedStudents
            { get; set; } = new();
    }

    // Provides a summary of an assigned module.
    public class ModuleLeaderModuleSummaryViewModel
    {
        public int ModuleId { get; set; }

        public string ModuleCode { get; set; } = string.Empty;

        public string ModuleName { get; set; } = string.Empty;

        public int StudentCount { get; set; }

        public double AttendancePercentage { get; set; }
    }

    // Provides attendance information for an individual student.
    public class ModuleLeaderStudentSummaryViewModel
    {
        public int StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string ModuleCode { get; set; } = string.Empty;

        public string ModuleName { get; set; } = string.Empty;

        public int TotalRecords { get; set; }

        public int AttendedCount { get; set; }

        public double AttendancePercentage { get; set; }

        // Number of consecutive teaching weeks without engagement.
        public int ConsecutiveWeeksWithoutEngagement { get; set; }
    }
}
