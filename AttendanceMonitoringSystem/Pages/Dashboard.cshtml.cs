
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AttendanceMonitoringSystem.Pages;

[Authorize]
public class DashboardModel : PageModel
{
    public string UserName { get; private set; } = "User";

    public List<string> Roles { get; private set; } = new();

    public void OnGet()
    {
        UserName = User.Identity?.Name ?? "User";

        Roles = User.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct()
            .ToList();
    }
}
