
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class ModuleLeaderAssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ModuleLeaderAssignmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var assignments = await _context.ModuleLeaderAssignments
                .AsNoTracking()
                .Include(a => a.ModuleLeader)
                .Include(a => a.Module)
                .OrderBy(a => a.ModuleLeader.Name)
                .ThenBy(a => a.Module.ModuleCode)
                .ToListAsync();

            await LoadDropdowns();

            return View(assignments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int moduleLeaderUserId, int moduleId)
        {
            var isModuleLeader = await _context.Users
                .AnyAsync(u =>
                    u.UserId == moduleLeaderUserId &&
                    u.IsActive &&
                    u.UserRoles.Any(ur =>
                        ur.Role.RoleName == "ModuleLeader"));

            var moduleExists = await _context.Modules
                .AnyAsync(m => m.ModuleId == moduleId);

            if (!isModuleLeader || !moduleExists)
            {
                TempData["Error"] = "Please select a valid active Module Leader and module.";
                return RedirectToAction(nameof(Index));
            }

            var alreadyAssigned = await _context.ModuleLeaderAssignments
                .AnyAsync(a =>
                    a.ModuleLeaderUserId == moduleLeaderUserId &&
                    a.ModuleId == moduleId);

            if (alreadyAssigned)
            {
                TempData["Error"] = "This Module Leader is already assigned to that module.";
                return RedirectToAction(nameof(Index));
            }

            _context.ModuleLeaderAssignments.Add(new ModuleLeaderAssignment
            {
                ModuleLeaderUserId = moduleLeaderUserId,
                ModuleId = moduleId
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Module assignment created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var assignment = await _context.ModuleLeaderAssignments
                .FindAsync(id);

            if (assignment == null)
            {
                TempData["Error"] = "Assignment not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.ModuleLeaderAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Module assignment removed.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns()
        {
            var moduleLeaders = await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive &&
                    u.UserRoles.Any(ur =>
                        ur.Role.RoleName == "ModuleLeader"))
                .OrderBy(u => u.Name)
                .ToListAsync();

            var modules = await _context.Modules
                .AsNoTracking()
                .OrderBy(m => m.ModuleCode)
                .ToListAsync();

            ViewBag.ModuleLeaders = new SelectList(
                moduleLeaders,
                "UserId",
                "Name");

            ViewBag.Modules = new SelectList(
                modules,
                "ModuleId",
                "ModuleCode");
        }
    }
}
