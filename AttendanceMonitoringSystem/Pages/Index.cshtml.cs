
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace AttendanceMonitoringSystem.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _hasher = new();

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var email = Email.Trim();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            ErrorMessage = "Email or password is incorrect.";
            return Page();
        }

        var result = _hasher.VerifyHashedPassword(
            user, user.PasswordHash, Password);

        if (result == PasswordVerificationResult.Failed)
        {
            ErrorMessage = "Email or password is incorrect.";
            return Page();
        }

        // Student accounts go to the student view.
        var isStudent = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == user.UserId
                && ur.Role.RoleName == "Student");

        if (!isStudent)
        {
            ErrorMessage = "This account is not assigned the Student role.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, "Student")
        };

        var identity = new ClaimsIdentity(claims, "MyCookieAuth");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("MyCookieAuth", principal);

        return RedirectToPage("/StudentDashboard");
    }

    public void OnGet() { }
}
