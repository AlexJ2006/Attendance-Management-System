using AttendanceMonitoringSystem.Models;

namespace AttendanceMonitoringSystem.Models;

public class AttendanceReportViewModel
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public int? CourseId { get; set; }
    public int? ModuleId { get; set; }

    public List<Course> Courses { get; set; } = new();
    public List<Module> Modules { get; set; } = new();

    public int TotalRecords { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int AuthorisedAbsenceCount { get; set; }
    public int LateCount { get; set; }
    public int LeftEarlyCount { get; set; }
    public int NotRecordedCount { get; set; }

    public double AttendancePercentage { get; set; }

    public List<AttendanceReportStudentViewModel> Students { get; set; } = new();
}

public class AttendanceReportStudentViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;

    public int TotalSessions { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int AuthorisedAbsenceCount { get; set; }
    public int LateCount { get; set; }
    public int LeftEarlyCount { get; set; }

    public double AttendancePercentage { get; set; }
}