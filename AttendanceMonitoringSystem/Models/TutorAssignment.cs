using System.ComponentModel.DataAnnotations;

namespace AttendanceMonitoringSystem.Models
{
    // Connects a Tutor (Lecturer) to a specific module occurrence.
    // This allows access to be controlled for each teaching occurrence.
    public class TutorAssignment
    {
        [Key]
        public int TutorAssignmentId { get; set; }

        // The UserId of the assigned Tutor.
        public int TutorUserId { get; set; }

        // The module occurrence the Tutor is assigned to.
        public int ModuleOccurrenceId { get; set; }

        // Navigation property for the Tutor's user account.
        public User Tutor { get; set; } = null!;

        // Navigation property for the assigned module occurrence.
        public ModuleOccurrence ModuleOccurrence { get; set; } = null!;
    }
}