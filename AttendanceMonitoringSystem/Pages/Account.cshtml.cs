using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Pages;

// Shows the signed-in user's details and lets them change their password.
public class AccountModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _hasher = new();

    public AccountModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public User? CurrentUser { get; private set; }
    public List<string> Roles { get; private set; } = new();

    [BindProperty] public string? CurrentPassword { get; set; }
    [BindProperty] public string? NewPassword { get; set; }
    [BindProperty] public string? ConfirmPassword { get; set; }

    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    public async Task OnGetAsync() => await LoadAsync();

    public async Task OnPostAsync()
    {
        await LoadAsync();

        if (CurrentUser == null)
        {
            ErrorMessage = "Your account could not be found.";
            return;
        }

        if (string.IsNullOrWhiteSpace(CurrentPassword) ||
            string.IsNullOrWhiteSpace(NewPassword) ||
            string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ErrorMessage = "Please fill in all required fields.";
            return;
        }

        if (string.IsNullOrEmpty(CurrentUser.PasswordHash) ||
            _hasher.VerifyHashedPassword(CurrentUser, CurrentUser.PasswordHash, CurrentPassword)
                == PasswordVerificationResult.Failed)
        {
            ErrorMessage = "Your current password is incorrect.";
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "The new passwords do not match.";
            return;
        }

        if (NewPassword.Length < 8 || !NewPassword.Any(char.IsUpper) ||
            !NewPassword.Any(char.IsLower) || !NewPassword.Any(char.IsDigit))
        {
            ErrorMessage = "The new password must be at least 8 characters and include an upper-case letter, a lower-case letter and a number.";
            return;
        }

        var user = await _context.Users.FirstAsync(u => u.UserId == CurrentUser.UserId);
        user.PasswordHash = _hasher.HashPassword(user, NewPassword);
        await _context.SaveChangesAsync();

        SuccessMessage = "Your password has been changed.";
    }

    private async Task LoadAsync()
    {
        var userId = AccessScopeService.GetUserId(User);
        if (userId == null) return;

        CurrentUser = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId.Value);

        Roles = await _context.UserRoles
            .Where(ur => ur.UserId == userId.Value)
            .Select(ur => ur.Role.RoleName)
            .ToListAsync();
    }
}
