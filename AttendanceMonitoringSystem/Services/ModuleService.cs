
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Services
{
    public class ModuleService
    {
        private readonly ApplicationDbContext _context;

        public ModuleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Module> GetAllModules()
        {
            return _context.Modules
                .AsNoTracking()
                .Include(m => m.Course)
                .OrderBy(m => m.ModuleCode)
                .ToList();
        }

        public Module? GetModuleById(int moduleId)
        {
            return _context.Modules
                .AsNoTracking()
                .Include(m => m.Course)
                .FirstOrDefault(m => m.ModuleId == moduleId);
        }
        public List<Module> GetModulesForModuleLeader(int userId)
        {
            return _context.Modules
                .AsNoTracking()
                .Where(m => _context.ModuleLeaderAssignments
                    .Any(a => a.ModuleId == m.ModuleId
                        && a.ModuleLeaderUserId == userId))
                .Include(m => m.Course)
                .OrderBy(m => m.ModuleCode)
                .ToList();
        }
    }
}
