
namespace AttendanceMonitoringSystem.Models;

// Holds the information displayed for each user account.
public class AdminUserViewModel
{
    // Unique identifier for the user.
    public int UserId { get; set; }

    // User's full name.
    public string Name { get; set; } = string.Empty;

    // User's email address.
    public string Email { get; set; } = string.Empty;

    // Indicates whether the account is active.
    public bool IsActive { get; set; }

    // All roles assigned to this user.
    public List<string> RoleNames { get; set; } = new();
}

// Holds the user list and the search term.
public class AdminUserListViewModel
{
    // Search text entered by the Administrator.
    public string SearchTerm { get; set; } = string.Empty;

    // Users matching the search.
    public List<AdminUserViewModel> Users { get; set; } = new();
}
