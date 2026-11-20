
namespace AttendanceMonitoringSystem.Models;

public class Attendance
{
    public int AttendanceId { get; set; }

    public int StudentId { get; set; }

    public int SessionId { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.NotRecorded;

    public DateTime RecordedAt { get; set; }

    public int RecordedByUserId { get; set; }

    public Student Student { get; set; } = null!;

    public Session Session { get; set; } = null!;

    public User RecordedByUser { get; set; } = null!;
}
