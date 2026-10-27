
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Pages;

public class ForgotPasswordModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordResetService _resetService;
    private readonly IWebHostEnvironment _environment;

    public ForgotPasswordModel(
        ApplicationDbContext context,
        PasswordResetService resetService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _resetService = resetService;
        _environment = environment;
    }

    [BindProperty]
    public string Email { get; set; } = "";

    public string? Message { get; private set; }

    public string? DemoCode { get; private set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var email = Email.Trim();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        Message = "If an active account exists for that email, reset instructions are available.";

        if (user != null)
        {
            var code = _resetService.CreateCode(user.Email);

            if (_environment.IsDevelopment())
            {
                TempData["DemoResetCode"] = code;
                TempData["ResetEmail"] = user.Email;
            }
        }

        // Always redirect, including for unknown email addresses.
        return RedirectToPage("/ResetPassword");
    }

    public void OnGet()
    {
        Message = "Enter your registered email address to request a reset code.";
    }
}
