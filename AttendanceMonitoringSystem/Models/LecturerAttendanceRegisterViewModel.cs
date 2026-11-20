using AttendanceMonitoringSystem.Models;

namespace AttendanceMonitoringSystem.Models;

public class LecturerAttendanceRegisterViewModel
{
    public int SessionId { get; set; }

    public int ModuleOccurrenceId { get; set; }

    public string ModuleCode { get; set; } = string.Empty;

    public string ModuleName { get; set; } = string.Empty;

    public string SessionType { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public SessionStatus SessionStatus { get; set; }

    public int PresentCount { get; set; }

    public int AbsentCount { get; set; }

    public int NotRecordedCount { get; set; }

    public List<LecturerAttendanceStudentViewModel> Students { get; set; }
        = new();
}

public class LecturerAttendanceStudentViewModel
{
    public int StudentId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int? AttendanceId { get; set; }

    public AttendanceStatus? Status { get; set; }
}