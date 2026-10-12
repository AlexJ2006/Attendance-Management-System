
namespace AttendanceMonitoringSystem.Models;

public class Student
{
    public int StudentId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int CourseId { get; set; }

    public int YearOfStudy { get; set; }

    public string Department { get; set; } = string.Empty;

    public Course Course { get; set; } = null!;

    public ICollection<Attendance> Attendances { get; set; } 
    = new List<Attendance>();
}
