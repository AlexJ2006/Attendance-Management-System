
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AttendanceMonitoringSystem.Controllers;

// Only Administrators can access user and account management.
[Authorize(Roles = "Administrator")]
public class AdminUsersController : Controller
{
    private readonly ApplicationDbContext _context;

    // Receive the existing database context through dependency injection.
    public AdminUsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Display all users, with an optional name or email search.
    public async Task<IActionResult> Index(string? search)
    {
        // Retrieve users without tracking because this page is read-only.
        var query = _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        // Filter the accounts if a search term was supplied.
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();

            query = query.Where(u =>
                u.Name.Contains(searchTerm) ||
                u.Email.Contains(searchTerm));
        }

        // Retrieve the matching accounts.
        var users = await query
            .OrderBy(u => u.Name)
            .ToListAsync();

        // Convert database records into safe display models.
        // Password hashes are never included.
        var model = new AdminUserListViewModel
        {
            SearchTerm = search?.Trim() ?? string.Empty,

            Users = users.Select(u => new AdminUserViewModel
            {
                UserId = u.UserId,
                Name = u.Name,
                Email = u.Email,
                IsActive = u.IsActive,

                // Display every role assigned to the account.
                RoleNames = u.UserRoles
                    .Select(ur => ur.Role.RoleName)
                    .OrderBy(role => role)
                    .ToList()
            }).ToList()
        };

        return View(model);
    }

    // Activate a previously deactivated user account.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int userId)
    {
        // Find the account that needs to be activated.
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            TempData["Error"] = "User account not found.";
            return RedirectToAction(nameof(Index));
        }

        // Avoid unnecessary database updates.
        if (user.IsActive)
        {
            TempData["Error"] = "This account is already active.";
            return RedirectToAction(nameof(Index));
        }

        // Reactivate the existing account.
        // Existing roles and attendance records are preserved.
        user.IsActive = true;

        await _context.SaveChangesAsync();

        TempData["Success"] = $"{user.Name}'s account has been activated.";

        return RedirectToAction(nameof(Index));
    }

    // Deactivate an existing user account.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int userId)
    {
        // Find the account that needs to be deactivated.
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            TempData["Error"] = "User account not found.";
            return RedirectToAction(nameof(Index));
        }

        // Avoid unnecessary database updates.
        if (!user.IsActive)
        {
            TempData["Error"] = "This account is already inactive.";
            return RedirectToAction(nameof(Index));
        }

        // Identify the Administrator who is currently signed in.
        var currentUserIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        int.TryParse(currentUserIdValue, out var currentUserId);

        // Prevent an Administrator from deactivating their own account.
        if (user.UserId == currentUserId)
        {
            TempData["Error"] =
                "You cannot deactivate your own account.";

            return RedirectToAction(nameof(Index));
        }

        // Check whether the account being deactivated is an Administrator.
        var isAdministrator = user.UserRoles
            .Any(ur => ur.Role.RoleName == "Administrator");

        if (isAdministrator)
        {
            // Count all currently active Administrator accounts.
            var activeAdministratorCount = await _context.Users
                .CountAsync(u =>
                    u.IsActive &&
                    u.UserRoles.Any(ur =>
                        ur.Role.RoleName == "Administrator"));

            // Never allow the last active Administrator to be deactivated.
            if (activeAdministratorCount <= 1)
            {
                TempData["Error"] =
                    "The last active Administrator account cannot be deactivated.";

                return RedirectToAction(nameof(Index));
            }
        }

        // Deactivate the account without deleting the user.
        // Existing roles and attendance records remain untouched.
        user.IsActive = false;

        await _context.SaveChangesAsync();

        TempData["Success"] = $"{user.Name}'s account has been deactivated.";

        return RedirectToAction(nameof(Index));
    }

    // Display the role management page for a particular user.
    public async Task<IActionResult> ManageRoles(int id)
    {
        var model = await BuildRoleManagementModel(id);

        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    // Assign an existing role to a user.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRole(int userId, int selectedRoleId)
    {
        // Check that the requested user exists.
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return NotFound();
        }

        // Check that the requested role exists.
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleId == selectedRoleId);

        if (role == null)
        {
            TempData["Error"] = "Please select a valid role.";
            return RedirectToAction(nameof(ManageRoles), new { id = userId });
        }

        // Prevent duplicate role assignments.
        var alreadyAssigned = await _context.UserRoles
            .AnyAsync(ur =>
                ur.UserId == userId &&
                ur.RoleId == selectedRoleId);

        if (alreadyAssigned)
        {
            TempData["Error"] = "This role is already assigned to the user.";
            return RedirectToAction(nameof(ManageRoles), new { id = userId });
        }

        // Create the user-to-role relationship.
        _context.UserRoles.Add(new UserRole
        {
            UserId = userId,
            RoleId = selectedRoleId,
            AssignedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = $"{role.RoleName} role assigned successfully.";

        return RedirectToAction(nameof(ManageRoles), new { id = userId });
    }

    // Remove an existing role from a user.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRole(int userId, int roleId)
    {
        // Find the requested user-to-role relationship.
        var assignment = await _context.UserRoles
            .Include(ur => ur.Role)
            .Include(ur => ur.User)
            .FirstOrDefaultAsync(ur =>
                ur.UserId == userId &&
                ur.RoleId == roleId);

        if (assignment == null)
        {
            TempData["Error"] = "The role assignment could not be found.";
            return RedirectToAction(nameof(ManageRoles), new { id = userId });
        }

        // Identify the account currently signed in.
        var currentUserIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        int.TryParse(currentUserIdValue, out var currentUserId);

        // Prevent an Administrator from removing their own Administrator role.
        if (assignment.Role.RoleName == "Administrator" &&
            assignment.UserId == currentUserId)
        {
            TempData["Error"] =
                "You cannot remove your own Administrator role.";

            return RedirectToAction(nameof(ManageRoles), new { id = userId });
        }

        // Count active Administrator accounts before removing a role.
        if (assignment.Role.RoleName == "Administrator" &&
            assignment.User.IsActive)
        {
            var activeAdministratorCount = await _context.UserRoles
                .CountAsync(ur =>
                    ur.Role.RoleName == "Administrator" &&
                    ur.User.IsActive);

            // Keep at least one active Administrator.
            if (activeAdministratorCount <= 1)
            {
                TempData["Error"] =
                    "The last active Administrator role cannot be removed.";

                return RedirectToAction(nameof(ManageRoles), new { id = userId });
            }
        }

        // Remove only the selected user-to-role relationship.
        _context.UserRoles.Remove(assignment);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"{assignment.Role.RoleName} role removed successfully.";

        return RedirectToAction(nameof(ManageRoles), new { id = userId });
    }

    // Build the information required by the role management page.
    private async Task<AdminUserRolesViewModel?> BuildRoleManagementModel(int userId)
    {
        // Load the selected user and their existing roles.
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return null;
        }

        // Find roles that are not already assigned to this user.
        var assignedRoleIds = user.UserRoles
            .Select(ur => ur.RoleId)
            .ToList();

        var availableRoles = await _context.Roles
            .AsNoTracking()
            .Where(r => !assignedRoleIds.Contains(r.RoleId))
            .OrderBy(r => r.RoleName)
            .ToListAsync();

        // Prepare the page model.
        return new AdminUserRolesViewModel
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive,

            AvailableRoles = availableRoles.Select(r =>
                new AdminRoleOptionViewModel
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName
                }).ToList(),

            AssignedRoles = user.UserRoles
                .OrderBy(ur => ur.Role.RoleName)
                .Select(ur => new AdminAssignedRoleViewModel
                {
                    RoleId = ur.RoleId,
                    RoleName = ur.Role.RoleName
                }).ToList()
        };
    }
}
