using System.Diagnostics;
using AttendanceMonitoringSystem.Models;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceMonitoringSystem.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // "/" sends signed-in users to their own dashboard and everyone else
    // to the sign-in page.
    public IActionResult Index()
    {
        return User.Identity?.IsAuthenticated == true
            ? Redirect(RoleHome.GetHomeUrl(User))
            : RedirectToPage("/Index");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // Shown instead of a stack trace when an unexpected error occurs,
    // for example when the database cannot be reached (NFR01).
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

        if (feature?.Error != null)
        {
            _logger.LogError(feature.Error,
                "Unhandled error on {Path}", feature.Path);
        }

        Response.StatusCode = StatusCodes.Status500InternalServerError;

        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    // Friendly pages for 403 / 404 and similar status codes.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status(int id)
    {
        if (id == StatusCodes.Status403Forbidden)
        {
            return View("AccessDenied");
        }

        ViewData["StatusCode"] = id;
        return View("StatusCode");
    }

    // Used by the cookie authentication handler when a signed-in user
    // requests something outside their role or academic scope (T07, T08).
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
