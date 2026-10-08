using AttendanceMonitoringSystem.Models;

namespace AttendanceMonitoringSystem.Services
{
    public class ModuleService
    {
        private static readonly List<Module> Modules = new()
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

        public List<Module> GetAllModules()
        {
            return Modules;
        }

        public Module? GetModuleById(int moduleId)
        {
            return Modules.FirstOrDefault(m => m.ModuleId == moduleId);
        }
    }
}