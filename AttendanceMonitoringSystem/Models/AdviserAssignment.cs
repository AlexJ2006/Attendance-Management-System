namespace AttendanceMonitoringSystem.Models
{
    public class AdviserAssignment
    {
        public int AdviserAssignmentId { get; set; }

        public int AdviserUserId { get; set; }

        public int StudentId { get; set; }

        public User Adviser { get; set; } = null!;

        public Student Student { get; set; } = null!;
    }
}