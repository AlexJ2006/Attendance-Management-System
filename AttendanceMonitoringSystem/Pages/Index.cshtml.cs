using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace AttendanceMonitoringSystem.Pages;

public class IndexModel : PageModel
{
    [BindProperty]
    public string Email { get; set; }

    [BindProperty]
    public string Password { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Temporary Module Leader account
        if (Email == "moduleleader@example.com" && Password == "Password123")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Email),
                new Claim(ClaimTypes.Role, "ModuleLeader")
            };

            var identity = new ClaimsIdentity(
                claims,
                "MyCookieAuth");

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                "MyCookieAuth",
                principal);

            return RedirectToAction("Index", "Students");
        }

        // Temporary Student account
        if (Email == "student@example.com" && Password == "Student123")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Email),
                new Claim(ClaimTypes.Role, "Student")
            };

            var identity = new ClaimsIdentity(
                claims,
                "MyCookieAuth");

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                "MyCookieAuth",
                principal);

            return RedirectToPage("/Index");
        }

        // Login failed
        ModelState.AddModelError(
            string.Empty,
            "Invalid email address or password.");

        return Page();
    }
}