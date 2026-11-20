namespace AttendanceMonitoringSystem.Models;

// A group of students who started together (ERD: Cohort).
public class Cohort
{
    public int CohortId { get; set; }

    public string CohortName { get; set; } = string.Empty;

    public int AcademicYear { get; set; }
}
