using AttendanceMonitoringSystem.Models;

namespace AttendanceMonitoringSystem.Models
{
    // Holds the information needed for the Course Leader's main dashboard.
    public class CourseLeaderDashboardViewModel
    {
        public int AttendanceWarningThreshold { get; set; } = 80;

        public List<CourseLeaderCourseSummaryViewModel> Courses { get; set; }
            = new();

        public List<CourseLeaderStudentSummaryViewModel> LowAttendanceStudents
            { get; set; } = new();
    }

    // Provides a summary of each course assigned to the signed-in Course Leader.
    public class CourseLeaderCourseSummaryViewModel
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int ModuleCount { get; set; }
        public int StudentCount { get; set; }
        public double AttendancePercentage { get; set; }
    }

    // Holds a student's attendance totals and percentage.
    public class CourseLeaderStudentSummaryViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public int AttendedCount { get; set; }
        public double AttendancePercentage { get; set; }
    }

    // Supplies the information displayed when a Course Leader opens a course.
    public class CourseLeaderCourseViewModel
    {
        public Course Course { get; set; } = null!;

        public List<Module> Modules { get; set; } = new();

        public List<CourseLeaderStudentSummaryViewModel> Students { get; set; }
            = new();

        public double AttendancePercentage { get; set; }
    }

    // Supplies a student's attendance history within their assigned course.
    public class CourseLeaderStudentAttendanceViewModel
    {
        public Student Student { get; set; } = null!;

        public List<Attendance> AttendanceRecords { get; set; } = new();

        public double AttendancePercentage { get; set; }
    }
}