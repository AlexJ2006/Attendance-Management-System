
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Controllers
{
    [Authorize(Roles = RoleNames.Administrator)]
    public class CourseLeaderAssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourseLeaderAssignmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var assignments = await _context.CourseLeaderAssignments
                .AsNoTracking()
                .Include(a => a.CourseLeader)
                .Include(a => a.Course)
                .OrderBy(a => a.CourseLeader.Name)
                .ThenBy(a => a.Course.CourseCode)
                .ToListAsync();

            await LoadDropdowns();

            return View(assignments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int courseLeaderUserId, int courseId)
        {
            var isCourseLeader = await _context.Users
                .AnyAsync(u => u.UserId == courseLeaderUserId
                    && u.IsActive
                    && u.UserRoles.Any(ur =>
                        ur.Role.RoleName == RoleNames.CourseLeader));

            var courseExists = await _context.Courses
                .AnyAsync(c => c.CourseId == courseId);

            if (!isCourseLeader || !courseExists)
            {
                TempData["Error"] =
                    "Please select a valid active Course Leader and course.";

                return RedirectToAction(nameof(Index));
            }

            var alreadyAssigned = await _context.CourseLeaderAssignments
                .AnyAsync(a => a.CourseLeaderUserId == courseLeaderUserId
                    && a.CourseId == courseId);

            if (alreadyAssigned)
            {
                TempData["Error"] =
                    "This Course Leader is already assigned to that course.";

                return RedirectToAction(nameof(Index));
            }

            _context.CourseLeaderAssignments.Add(new CourseLeaderAssignment
            {
                CourseLeaderUserId = courseLeaderUserId,
                CourseId = courseId
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Course Leader assignment created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var assignment = await _context.CourseLeaderAssignments
                .FindAsync(id);

            if (assignment == null)
            {
                TempData["Error"] = "Assignment not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.CourseLeaderAssignments.Remove(assignment);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Course Leader assignment removed.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns()
        {
            var courseLeaders = await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive &&
                    u.UserRoles.Any(ur =>
                        ur.Role.RoleName == RoleNames.CourseLeader))
                .OrderBy(u => u.Name)
                .ToListAsync();

            var courses = await _context.Courses
                .AsNoTracking()
                .OrderBy(c => c.CourseCode)
                .ToListAsync();

            ViewBag.CourseLeaders =
                new SelectList(courseLeaders, "UserId", "Name");

            ViewBag.Courses =
                new SelectList(courses, "CourseId", "CourseCode");
        }
    }
}
