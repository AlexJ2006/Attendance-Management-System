
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

        Console.WriteLine($"LOGIN ATTEMPT: Email received = [{email}]");
        Console.WriteLine($"LOGIN ATTEMPT: Password length = {Password?.Length ?? 0}");


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

        var roleNames = await _context.UserRoles
            .Where(ur => ur.UserId == user.UserId)
            .Select(ur => ur.Role.RoleName)
            .Distinct()
            .ToListAsync();

        if (roleNames.Count == 0)
        {
            ErrorMessage = "This account has no assigned roles. Please contact an administrator.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email)
        };

        foreach (var roleName in roleNames)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName));
        }

        var identity = new ClaimsIdentity(claims, "MyCookieAuth");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("MyCookieAuth", principal);

        // Send Administrators to their own dashboard first.
        // Users who also have Student access keep that role.
        if (roleNames.Contains("Administrator"))
        {
            return RedirectToPage("/AdminDashboard");
        }

        // Keep the existing destination for other users.
        return RedirectToPage("/Dashboard");
        }

    public void OnGet() { }
}
