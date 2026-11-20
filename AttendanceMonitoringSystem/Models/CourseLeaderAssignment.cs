
namespace AttendanceMonitoringSystem.Models
{
    public class CourseLeaderAssignment
    {
        public int CourseLeaderAssignmentId { get; set; }

        public int CourseLeaderUserId { get; set; }

        public int CourseId { get; set; }

        public User CourseLeader { get; set; } = null!;

        public Course Course { get; set; } = null!;
    }
}

