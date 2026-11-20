using System.Security.Claims;
using AttendanceMonitoringSystem.Models;

namespace AttendanceMonitoringSystem.Services;

// Decides which dashboard a user sees after signing in.
// Users with several roles land on their most senior role's dashboard
// and can reach the others from the navigation bar.
public static class RoleHome
{
    public static string GetHomeUrl(ClaimsPrincipal user)
    {
        if (user.IsInRole(RoleNames.Administrator)) return "/AdminDashboard";
        if (user.IsInRole(RoleNames.CourseLeader)) return "/CourseLeader";
        if (user.IsInRole(RoleNames.ModuleLeader)) return "/ModuleLeader";
        if (user.IsInRole(RoleNames.Tutor)) return "/Tutor";
        if (user.IsInRole(RoleNames.Adviser)) return "/Students/AssignedToMe";
        if (user.IsInRole(RoleNames.Student)) return "/Dashboard";

        return "/Index";
    }
}
