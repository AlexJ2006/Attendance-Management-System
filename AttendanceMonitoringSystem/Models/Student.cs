
namespace AttendanceMonitoringSystem.Models;

public class Student
{
    public int StudentId { get; set; }

    // Links the student record to the account used to sign in.
    // Attendance is always looked up from the signed-in user's ID,
    // never from a value supplied in the URL.
    public int UserId { get; set; }

    public int CohortId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int CourseId { get; set; }

    public int YearOfStudy { get; set; }

    public string Department { get; set; } = string.Empty;

    public Course Course { get; set; } = null!;

    public User? User { get; set; }

    public ICollection<Attendance> Attendances { get; set; } 
    = new List<Attendance>();
}
