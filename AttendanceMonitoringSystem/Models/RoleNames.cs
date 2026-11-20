namespace AttendanceMonitoringSystem.Models;

// Role names used in [Authorize] attributes and role checks.
// Keeping them in one place avoids spelling mistakes.
public static class RoleNames
{
    public const string Student = "Student";
    public const string Tutor = "Tutor";
    public const string ModuleLeader = "ModuleLeader";
    public const string CourseLeader = "CourseLeader";
    public const string Adviser = "Adviser";
    public const string Administrator = "Administrator";

    // Any member of staff who can view attendance information.
    public const string AllStaff =
        Tutor + "," + ModuleLeader + "," + CourseLeader + "," +
        Adviser + "," + Administrator;

    // Friendly names shown in the user interface.
    public static string Display(string roleName) => roleName switch
    {
        ModuleLeader => "Module Leader",
        CourseLeader => "Course Leader",
        Adviser => "Academic Adviser",
        _ => roleName
    };
}
