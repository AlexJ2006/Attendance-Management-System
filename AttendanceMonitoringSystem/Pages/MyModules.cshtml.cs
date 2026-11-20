using System.Security.Claims;
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Pages;

[Authorize(Roles = "Student")]
public class MyModulesModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public MyModulesModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public string StudentName { get; private set; } = "Student";
    public string CourseName { get; private set; } = "";
    public string CourseCode { get; private set; } = "";
    public string? Message { get; private set; }

    public List<Module> Modules { get; private set; } = new();

    public async Task OnGetAsync()
    {
        StudentName = User.Identity?.Name ?? "Student";

        string email = User.FindFirstValue(ClaimTypes.Email) ?? "";

        if (string.IsNullOrWhiteSpace(email))
        {
            Message = "Your account email could not be found.";
            return;
        }

        var student = await _context.Students
            .AsNoTracking()
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Email == email);

        if (student == null)
        {
            Message = "No student record is linked to your account.";
            return;
        }

        CourseName = student.Course?.CourseName ?? "Unknown course";
        CourseCode = student.Course?.CourseCode ?? "";

        Modules = await _context.Modules
            .AsNoTracking()
            .Where(m => m.CourseId == student.CourseId)
            .Include(m => m.ModuleOccurrences)
            .OrderBy(m => m.ModuleCode)
            .ToListAsync();

        if (Modules.Count == 0)
        {
            Message = "There are currently no modules listed for your course.";
        }
    }
}
