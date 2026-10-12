
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
                CourseId = 1,
                YearOfStudy = 3
            },

            new Module
            {
                ModuleId = 2,
                ModuleCode = "CS201",
                ModuleName = "Database Systems",
                CourseId = 1,
                YearOfStudy = 2
            },

            new Module
            {
                ModuleId = 3,
                ModuleCode = "CS302",
                ModuleName = "Web Application Development",
                CourseId = 1,
                YearOfStudy = 3
            }
        };

        public List<Module> GetAllModules()
        {
            return Modules.ToList();
        }

        public Module? GetModuleById(int moduleId)
        {
            return Modules.FirstOrDefault(m => m.ModuleId == moduleId);
        }
    }
}
