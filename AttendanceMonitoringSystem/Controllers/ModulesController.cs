using System.Security.Claims;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceMonitoringSystem.Controllers
{
    [Authorize(Roles = "ModuleLeader")]
    public class ModulesController : Controller
    {
        private readonly ModuleService _moduleService;

        public ModulesController(ModuleService moduleService)
        {
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
            {
                return Forbid();
            }

            var modules = _moduleService.GetModulesForModuleLeader(userId);

            return View(modules);
        }
    }
}