namespace AttendanceMonitoringSystem.Models;

public class User
{
    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string? PasswordHash { get; set; }

    // Number of consecutive failed sign-in attempts (reset on success).
    public int FailedLoginAttempts { get; set; }

    // When set and in the future, the account is temporarily locked.
    public DateTime? LockoutEndUtc { get; set; }

    public ICollection<UserRole> UserRoles { get; set; }
        = new List<UserRole>();
}