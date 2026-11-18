
namespace AttendanceMonitoringSystem.Models;

// Holds the account information and role information
// displayed on the Administrator's Manage Roles page.
public class AdminUserRolesViewModel
{
    // Identifies the user being managed.
    public int UserId { get; set; }

    // Stores the user's name.
    public string Name { get; set; } = string.Empty;

    // Stores the user's email address.
    public string Email { get; set; } = string.Empty;

    // Indicates whether the account is active.
    public bool IsActive { get; set; }

    // Stores the selected role from the dropdown.
    public int SelectedRoleId { get; set; }

    // Contains roles that can be assigned.
    public List<AdminRoleOptionViewModel> AvailableRoles { get; set; } = new();

    // Contains roles already assigned.
    public List<AdminAssignedRoleViewModel> AssignedRoles { get; set; } = new();
}

// Represents a role available for assignment.
public class AdminRoleOptionViewModel
{
    // Identifies the role.
    public int RoleId { get; set; }

    // Stores the role name.
    public string RoleName { get; set; } = string.Empty;
}

// Represents a role currently assigned to the user.
public class AdminAssignedRoleViewModel
{
    // Identifies the role.
    public int RoleId { get; set; }

    // Stores the role name.
    public string RoleName { get; set; } = string.Empty;
}
