
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Controllers
{
    // Only Administrators can manage Module Leader assignments.
    [Authorize(Roles = "Administrator")]
    public class ModuleLeaderAssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Receive the existing database context.
        public ModuleLeaderAssignmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display existing assignments and load the dropdown lists.
        public async Task<IActionResult> Index()
        {
            // Retrieve assignments with the associated lecturer and module.
            var assignments = await _context.ModuleLeaderAssignments
                .AsNoTracking()
                .Include(a => a.ModuleLeader)
                .Include(a => a.Module)
                .OrderBy(a => a.ModuleLeader.Name)
                .ThenBy(a => a.Module.ModuleCode)
                .ToListAsync();

            // Load active Lecturers and available modules.
            await LoadDropdowns();

            return View(assignments);
        }

        // Create a new Module Leader assignment.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int moduleLeaderUserId, int moduleId)
        {
            // Check that the selected user is an active Lecturer.
            var isModuleLeader = await _context.Users
                .AnyAsync(u =>
                    u.UserId == moduleLeaderUserId &&
                    u.IsActive &&
                    u.UserRoles.Any(ur =>
                        ur.Role.RoleName == "Lecturer"));

            // Check that the selected module exists.
            var moduleExists = await _context.Modules
                .AnyAsync(m => m.ModuleId == moduleId);

            // Reject invalid users or modules.
            if (!isModuleLeader || !moduleExists)
            {
                TempData["Error"] =
                    "Please select a valid active Lecturer and module.";

                return RedirectToAction(nameof(Index));
            }

            // Prevent the same Lecturer being assigned to the same module twice.
            var alreadyAssigned = await _context.ModuleLeaderAssignments
                .AnyAsync(a =>
                    a.ModuleLeaderUserId == moduleLeaderUserId &&
                    a.ModuleId == moduleId);

            if (alreadyAssigned)
            {
                TempData["Error"] =
                    "This Lecturer is already assigned to that module.";

                return RedirectToAction(nameof(Index));
            }

            // Save the new assignment using the existing database table.
            _context.ModuleLeaderAssignments.Add(new ModuleLeaderAssignment
            {
                ModuleLeaderUserId = moduleLeaderUserId,
                ModuleId = moduleId
            });

            await _context.SaveChangesAsync();

            // Display a confirmation message.
            TempData["Success"] =
                "Module Leader assignment created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Remove an existing Module Leader assignment.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Find the selected assignment.
            var assignment = await _context.ModuleLeaderAssignments
                .FindAsync(id);

            // Handle an assignment that cannot be found.
            if (assignment == null)
            {
                TempData["Error"] = "Assignment not found.";

                return RedirectToAction(nameof(Index));
            }

            // Remove only the assignment, not the user or module.
            _context.ModuleLeaderAssignments.Remove(assignment);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Assignment removed successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Populate the Lecturer and Module dropdown lists.
        private async Task LoadDropdowns()
        {
            // Retrieve active users who have the existing Lecturer role.
            var moduleLeaders = await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive &&
                    u.UserRoles.Any(ur =>
                        ur.Role.RoleName == "Lecturer"))
                .OrderBy(u => u.Name)
                .ToListAsync();

            // Retrieve available modules.
            var modules = await _context.Modules
                .AsNoTracking()
                .OrderBy(m => m.ModuleCode)
                .ToListAsync();

            // Make the Lecturer list available to the view.
            ViewBag.ModuleLeaders = new SelectList(
                moduleLeaders,
                "UserId",
                "Name");

            // Make the Module list available to the view.
            ViewBag.Modules = new SelectList(
                modules,
                "ModuleId",
                "ModuleCode");
        }
    }
}
