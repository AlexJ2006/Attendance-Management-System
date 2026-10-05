using AttendanceMonitoringSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceMonitoringSystem.Controllers
{
    [Authorize(Roles = "ModuleLeader")]
    public class ModulesController : Controller
    {
        private static List<Module> Modules = new()
        {
            new Module
            {
                ModuleId = 1,
                ModuleCode = "SE301",
                ModuleName = "Software Engineering",
                Course = "Software Engineering",
                YearOfStudy = 3,
                ModuleLeader = "Geoff Briggs"
            },

            new Module
            {
                ModuleId = 2,
                ModuleCode = "CS201",
                ModuleName = "Database Systems",
                Course = "Software Engineering",
                YearOfStudy = 2,
                ModuleLeader = "Michael Smithson"
            },

            new Module
            {
                ModuleId = 3,
                ModuleCode = "CS302",
                ModuleName = "Web Application Development",
                Course = "Software Engineering",
                YearOfStudy = 3,
                ModuleLeader = "Geoff Briggs"
            }
        };

        public IActionResult Index()
        {
            return View(Modules);
        }
    }
}