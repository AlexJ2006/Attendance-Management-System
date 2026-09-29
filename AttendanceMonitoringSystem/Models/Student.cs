namespace AttendanceMonitoringSystem.Models
{
    public class Student
    {
        public int StudentId { get; set; }

        public int UserId { get; set; }

        public string StudentNumber { get; set; } = string.Empty;

        public User User { get; set; } = null!;
    }
}