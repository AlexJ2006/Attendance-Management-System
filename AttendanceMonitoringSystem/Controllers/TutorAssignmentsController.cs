using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Controllers
{
    // Only Administrators can manage Tutor assignments.
    [Authorize(Roles = "Administrator")]
    public class TutorAssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Receive the existing database context.
        public TutorAssignmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display existing Tutor assignments and dropdown lists.
        public async Task<IActionResult> Index()
        {
            // Retrieve assignments with Tutor and module occurrence details.
            var assignments = await _context.TutorAssignments
                .AsNoTracking()
                .Include(a => a.Tutor)
                .Include(a => a.ModuleOccurrence)
                    .ThenInclude(mo => mo.Module)
                .OrderBy(a => a.Tutor.Name)
                .ThenBy(a => a.ModuleOccurrence.Module.ModuleCode)
                .ToListAsync();

            await LoadDropdowns();

            return View(assignments);
        }

        // Assign an active Lecturer to a module occurrence.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int tutorUserId,
            int moduleOccurrenceId)
        {
            // Confirm that the selected Tutor is an active Lecturer.
            var isValidTutor = await _context.Users
                .AnyAsync(u =>
                    u.UserId == tutorUserId &&
                    u.IsActive &&
                    u.UserRoles.Any(ur =>
                        ur.Role.RoleName == RoleNames.Tutor));

            // Confirm that the selected module occurrence exists.
            var occurrenceExists = await _context.ModuleOccurrences
                .AnyAsync(mo =>
                    mo.ModuleOccurrenceId == moduleOccurrenceId);

            // Reject invalid selections.
            if (!isValidTutor || !occurrenceExists)
            {
                TempData["Error"] =
                    "Please select a valid active Tutor and module occurrence.";

                return RedirectToAction(nameof(Index));
            }

            // Prevent duplicate Tutor assignments.
            var alreadyAssigned = await _context.TutorAssignments
                .AnyAsync(a =>
                    a.TutorUserId == tutorUserId &&
                    a.ModuleOccurrenceId == moduleOccurrenceId);

            if (alreadyAssigned)
            {
                TempData["Error"] =
                    "This Tutor is already assigned to that occurrence.";

                return RedirectToAction(nameof(Index));
            }

            // Save the new assignment.
            _context.TutorAssignments.Add(new TutorAssignment
            {
                TutorUserId = tutorUserId,
                ModuleOccurrenceId = moduleOccurrenceId
            });

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Tutor assignment created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Remove an assignment without deleting the Tutor or occurrence.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var assignment = await _context.TutorAssignments
                .FindAsync(id);

            if (assignment == null)
            {
                TempData["Error"] = "Assignment not found.";

                return RedirectToAction(nameof(Index));
            }

            _context.TutorAssignments.Remove(assignment);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Tutor assignment removed successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Load active Lecturers and module occurrences for the dropdowns.
        private async Task LoadDropdowns()
        {
            // Only active users with the existing Lecturer role can be Tutors.
            var tutors = await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive &&
                    u.UserRoles.Any(ur =>
                        ur.Role.RoleName == RoleNames.Tutor))
                .OrderBy(u => u.Name)
                .ToListAsync();

            // Load occurrences together with their associated modules.
            var occurrences = await _context.ModuleOccurrences
                .AsNoTracking()
                .Include(mo => mo.Module)
                .OrderBy(mo => mo.Module.ModuleCode)
                .ThenBy(mo => mo.AcademicYear)
                .ThenBy(mo => mo.Semester)
                .ToListAsync();

            // Prepare readable occurrence descriptions for the dropdown.
            var occurrenceOptions = occurrences.Select(mo =>
                new SelectListItem
                {
                    Value = mo.ModuleOccurrenceId.ToString(),
                    Text = $"{mo.Module.ModuleCode} - " +
                           $"{mo.Module.ModuleName} - " +
                           $"{mo.AcademicYear} - {mo.Semester}"
                }).ToList();

            ViewBag.Tutors = new SelectList(
                tutors,
                "UserId",
                "Name");

            ViewBag.Occurrences = new SelectList(
                occurrenceOptions,
                "Value",
                "Text");
        }
    }
}