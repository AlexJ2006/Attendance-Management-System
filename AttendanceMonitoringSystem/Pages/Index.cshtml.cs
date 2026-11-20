using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AttendanceMonitoringSystem.Pages;

// UC01 - User Authentication (tests T01, T02, T03).
[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IndexModel> _logger;
    private readonly PasswordHasher<User> _hasher = new();

    public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        // Signed-in users go straight to their own dashboard.
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(RoleHome.GetHomeUrl(User));
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var emailMissing = string.IsNullOrWhiteSpace(Email);
        var passwordMissing = string.IsNullOrWhiteSpace(Password);

        // Figure 3.2.1c - empty field error.
        if (emailMissing || passwordMissing)
        {
            ErrorMessage = emailMissing && passwordMissing
                ? "Please fill in all required fields."
                : emailMissing
                    ? "Please enter your email address."
                    : "Please enter your password.";

            return Page();
        }

        var email = Email.Trim();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        // Figure 3.2.1b - the same message is used for an unknown email and
        // a wrong password so attackers cannot discover which emails exist.
        const string invalidMessage = "Invalid email or password. Please try again.";

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            _logger.LogWarning("Failed sign-in for unknown or unset account.");
            ErrorMessage = invalidMessage;
            return Page();
        }

        // Account lockout after repeated failures (STRIDE: spoofing).
        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            var minutes = (int)Math.Ceiling(
                (user.LockoutEndUtc.Value - DateTime.UtcNow).TotalMinutes);

            ErrorMessage =
                $"This account is temporarily locked after too many failed attempts. " +
                $"Please try again in {minutes} minute{(minutes == 1 ? "" : "s")}.";

            return Page();
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, Password);

        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= AuthConstants.MaxFailedLoginAttempts)
            {
                user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(AuthConstants.LockoutMinutes);
                user.FailedLoginAttempts = 0;

                ErrorMessage =
                    $"Too many failed attempts. This account has been locked for " +
                    $"{AuthConstants.LockoutMinutes} minutes.";
            }
            else
            {
                ErrorMessage = invalidMessage;
            }

            await _context.SaveChangesAsync();
            _logger.LogWarning("Failed sign-in for user {UserId}.", user.UserId);
            return Page();
        }

        // Figure 3.2.1d - deactivated account. Only shown once the password
        // is correct, so it does not reveal anything to an attacker.
        if (!user.IsActive)
        {
            ErrorMessage =
                "This account has been deactivated. Please contact I.T. Support.";
            return Page();
        }

        var roleNames = await _context.UserRoles
            .Where(ur => ur.UserId == user.UserId)
            .Select(ur => ur.Role.RoleName)
            .Distinct()
            .ToListAsync();

        if (roleNames.Count == 0)
        {
            ErrorMessage =
                "This account has no assigned roles. Please contact I.T. Support.";
            return Page();
        }

        // Successful sign-in resets the failure counter.
        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;

        // Upgrade older password hashes when the hasher recommends it.
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, Password);
        }

        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email)
        };

        claims.AddRange(roleNames.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, AuthConstants.Scheme);

        await HttpContext.SignInAsync(
            AuthConstants.Scheme, new ClaimsPrincipal(identity));

        _logger.LogInformation("User {UserId} signed in.", user.UserId);

        // Return to the page the user originally asked for, if it is local.
        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) &&
            ReturnUrl != "/" && !ReturnUrl.StartsWith("/Index", StringComparison.OrdinalIgnoreCase))
        {
            return LocalRedirect(ReturnUrl);
        }

        // Otherwise send the user to the dashboard for their main role.
        return Redirect(RoleHome.GetHomeUrl(new ClaimsPrincipal(identity)));
    }
}
