namespace AttendanceMonitoringSystem.Models
{
    public class AttendanceHistory
    {
        public int AttendanceHistoryId { get; set; }

        public int AttendanceId { get; set; }

        public string PreviousStatus { get; set; } = string.Empty;

        public string NewStatus { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; }

        public int ChangedByUserId { get; set; }

        public string Reason { get; set; } = string.Empty;

        public Attendance Attendance { get; set; } = null!;

        public User ChangedByUser { get; set; } = null!;
    }
}