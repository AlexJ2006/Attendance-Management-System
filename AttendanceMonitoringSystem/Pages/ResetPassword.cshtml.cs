
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Pages;

[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class ResetPasswordModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordResetService _resetService;
    private readonly IWebHostEnvironment _environment;
    private readonly PasswordHasher<User> _hasher = new();

    public ResetPasswordModel(
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

    [BindProperty]
    public string Code { get; set; } = "";

    [BindProperty]
    public string NewPassword { get; set; } = "";

    [BindProperty]
    public string ConfirmPassword { get; set; } = "";

    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }
    public string? DemoCode { get; private set; }

    public void OnGet()
    {
        Email = TempData.Peek("ResetEmail") as string ?? "";
        DemoCode = _environment.IsDevelopment()
            ? TempData.Peek("DemoResetCode") as string
            : null;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (NewPassword.Length < 8)
        {
            ErrorMessage = "Your new password must be at least 8 characters long.";
            return PageWithDemoCode();
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "The passwords do not match.";
            return PageWithDemoCode();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Email == Email.Trim() && u.IsActive);

        if (user == null ||
            !_resetService.ValidateAndConsumeCode(user.Email, Code.Trim()))
        {
            ErrorMessage = "The reset code is invalid or has expired. Request a new code.";
            return PageWithDemoCode();
        }

        user.PasswordHash = _hasher.HashPassword(user, NewPassword);
        await _context.SaveChangesAsync();

        TempData.Remove("DemoResetCode");
        TempData.Remove("ResetEmail");

        SuccessMessage = "Your password has been reset. You can now sign in.";
        return RedirectToPage("/Index", new { reset = true });
    }

    private PageResult PageWithDemoCode()
    {
        DemoCode = _environment.IsDevelopment()
            ? TempData.Peek("DemoResetCode") as string
            : null;

        return Page();
    }
}
