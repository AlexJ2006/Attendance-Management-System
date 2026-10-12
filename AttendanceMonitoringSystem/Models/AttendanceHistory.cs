
namespace AttendanceMonitoringSystem.Models;

public class AttendanceHistory
{
    public int AttendanceHistoryId { get; set; }

    public int AttendanceId { get; set; }

    public AttendanceStatus PreviousStatus { get; set; }

    public AttendanceStatus NewStatus { get; set; }

    public DateTime ChangedAt { get; set; }

    public int ChangedByUserId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public Attendance Attendance { get; set; } = null!;

    public User ChangedByUser { get; set; } = null!;
}
