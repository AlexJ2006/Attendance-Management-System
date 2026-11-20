
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

        if (user == null)
        {
            Message = "No active account was found for that email address.";
            return Page();
        }

        var code = _resetService.CreateCode(user.Email);

        DemoCode = code;
        Message = "Your reset code has been generated.";

        TempData["ResetEmail"] = user.Email;
        TempData["DemoResetCode"] = code;

        return Page();
    }

    public void OnGet()
    {
        Message = "Enter your registered email address to request a reset code.";
    }
}
