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
            var modules = _moduleService.GetAllModules();

            return View(modules);
        }
    }
}