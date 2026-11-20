
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AttendanceMonitoringSystem.Controllers;

// Only Administrators can access user and account management.
[Authorize(Roles = RoleNames.Administrator)]
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
            TempData["Error"] = "Account not found. It may have been removed.";
            return RedirectToAction(nameof(Index));
        }

        // Avoid unnecessary database updates.
        if (user.IsActive)
        {
            TempData["Error"] = "This account is already activated.";
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
            TempData["Error"] = "Account not found. It may have been removed.";
            return RedirectToAction(nameof(Index));
        }

        // Avoid unnecessary database updates.
        if (!user.IsActive)
        {
            TempData["Error"] = "This account is already deactivated.";
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
                "Operation not possible: you cannot deactivate your own account.";

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

    // UC06 - create a user account (figure 3.2.5a, test T11).
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new AdminUserFormViewModel();
        await PopulateFormOptionsAsync(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminUserFormViewModel model)
    {
        model.UserId = null;
        await ValidateFormAsync(model);

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "An initial password is required.");
        }
        else if (!IsStrongPassword(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password),
                "The password must be at least 8 characters and include an upper-case letter, a lower-case letter and a number.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateFormOptionsAsync(model);
            return View("Form", model);
        }

        var user = new User
        {
            Name = $"{model.FirstName.Trim()} {model.LastName.Trim()}",
            Email = model.Email.Trim(),
            IsActive = model.IsActive
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, model.Password!);

        // Save the user, roles and student record together so a failure
        // never leaves a half-created account (T11: dataset not corrupted).
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            foreach (var roleId in model.SelectedRoleIds.Distinct())
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.UserId,
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow
                });
            }

            if (await IncludesStudentRoleAsync(model.SelectedRoleIds))
            {
                _context.Students.Add(new Student
                {
                    UserId = user.UserId,
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    Email = user.Email,
                    CourseId = model.CourseId!.Value,
                    CohortId = model.CohortId!.Value,
                    YearOfStudy = model.YearOfStudy!.Value,
                    Department = model.Department.Trim()
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty,
                "The account could not be created. No changes have been saved. Please check the details and try again.");
            await PopulateFormOptionsAsync(model);
            return View("Form", model);
        }

        TempData["Success"] = $"The account for {user.Name} has been created and is " +
                              (user.IsActive ? "active." : "inactive.");

        return RedirectToAction(nameof(Index));
    }

    // UC06 - update an existing user's details.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            // Figure 3.2.5d - account not found.
            TempData["Error"] = "Account not found. It may have been removed.";
            return RedirectToAction(nameof(Index));
        }

        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == id);

        var nameParts = user.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        var model = new AdminUserFormViewModel
        {
            UserId = user.UserId,
            FirstName = student?.FirstName ?? nameParts.ElementAtOrDefault(0) ?? "",
            LastName = student?.LastName ?? nameParts.ElementAtOrDefault(1) ?? "",
            Email = user.Email,
            IsActive = user.IsActive,
            Department = student?.Department ?? "Staff",
            SelectedRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList(),
            HasStudentRecord = student != null,
            CourseId = student?.CourseId,
            CohortId = student?.CohortId,
            YearOfStudy = student?.YearOfStudy
        };

        await PopulateFormOptionsAsync(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminUserFormViewModel model)
    {
        model.UserId = id;

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            TempData["Error"] = "Account not found. It may have been removed.";
            return RedirectToAction(nameof(Index));
        }

        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == id);
        model.HasStudentRecord = student != null;

        // Roles are changed on the Manage Roles page, not here.
        model.SelectedRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();

        await ValidateFormAsync(model);

        // Figure 3.2.5g - an Administrator cannot deactivate themselves.
        if (!model.IsActive && user.UserId == AccessScopeService.GetUserId(User))
        {
            ModelState.AddModelError(nameof(model.IsActive),
                "Operation not possible: you cannot deactivate your own account.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateFormOptionsAsync(model);
            return View("Form", model);
        }

        user.Name = $"{model.FirstName.Trim()} {model.LastName.Trim()}";
        user.Email = model.Email.Trim();
        user.IsActive = model.IsActive;

        if (student != null)
        {
            student.FirstName = model.FirstName.Trim();
            student.LastName = model.LastName.Trim();
            student.Email = user.Email;
            student.Department = model.Department.Trim();
            student.CourseId = model.CourseId!.Value;
            student.CohortId = model.CohortId!.Value;
            student.YearOfStudy = model.YearOfStudy!.Value;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty,
                "The account could not be updated. No changes have been saved.");
            await PopulateFormOptionsAsync(model);
            return View("Form", model);
        }

        TempData["Success"] = $"{user.Name}'s account has been updated.";
        return RedirectToAction(nameof(Index));
    }

    // Server-side checks shared by Create and Edit (T11).
    private async Task ValidateFormAsync(AdminUserFormViewModel model)
    {
        var email = model.Email?.Trim() ?? "";

        if (email.Length > 0 && await _context.Users.AnyAsync(u =>
                u.Email == email && u.UserId != (model.UserId ?? 0)))
        {
            ModelState.AddModelError(nameof(model.Email),
                "An account with this email address already exists.");
        }

        if (!model.IsEdit && model.SelectedRoleIds.Count == 0)
        {
            ModelState.AddModelError(nameof(model.SelectedRoleIds),
                "Select at least one role.");
        }

        var validRoleIds = await _context.Roles.Select(r => r.RoleId).ToListAsync();
        if (model.SelectedRoleIds.Any(id => !validRoleIds.Contains(id)))
        {
            ModelState.AddModelError(nameof(model.SelectedRoleIds), "Please select valid roles only.");
        }

        var needsStudentDetails = model.IsEdit
            ? model.HasStudentRecord
            : await IncludesStudentRoleAsync(model.SelectedRoleIds);

        if (needsStudentDetails)
        {
            if (model.CourseId == null || !await _context.Courses.AnyAsync(c => c.CourseId == model.CourseId))
                ModelState.AddModelError(nameof(model.CourseId), "Course is required for students.");
            if (model.CohortId == null || !await _context.Cohorts.AnyAsync(c => c.CohortId == model.CohortId))
                ModelState.AddModelError(nameof(model.CohortId), "Cohort is required for students.");
            if (model.YearOfStudy == null)
                ModelState.AddModelError(nameof(model.YearOfStudy), "Year of study is required for students.");
        }
    }

    private async Task<bool> IncludesStudentRoleAsync(IEnumerable<int> roleIds)
    {
        var ids = roleIds.ToList();
        return await _context.Roles.AnyAsync(r =>
            ids.Contains(r.RoleId) && r.RoleName == RoleNames.Student);
    }

    private static bool IsStrongPassword(string password) =>
        password.Length >= 8 &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsLower) &&
        password.Any(char.IsDigit);

    private async Task PopulateFormOptionsAsync(AdminUserFormViewModel model)
    {
        model.AvailableRoles = await _context.Roles.AsNoTracking().OrderBy(r => r.RoleId).ToListAsync();

        model.Courses = await _context.Courses.AsNoTracking()
            .OrderBy(c => c.CourseName)
            .Select(c => new SelectListItem(c.CourseName + " (" + c.CourseCode + ")", c.CourseId.ToString()))
            .ToListAsync();

        model.Cohorts = await _context.Cohorts.AsNoTracking()
            .OrderBy(c => c.CohortName)
            .Select(c => new SelectListItem(c.CohortName, c.CohortId.ToString()))
            .ToListAsync();
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
