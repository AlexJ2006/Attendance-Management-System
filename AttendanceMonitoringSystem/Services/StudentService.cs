
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Services
{
    public class StudentService
    {
        private readonly ApplicationDbContext _context;

        public StudentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Student> GetAllStudents()
        {
            return _context.Students
                .AsNoTracking()
                .Include(s => s.Course)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();
        }

        public Student? GetStudentById(int studentId)
        {
            return _context.Students
                .AsNoTracking()
                .Include(s => s.Course)
                .FirstOrDefault(s => s.StudentId == studentId);
        }

        public List<Student> GetStudentsForAdviser(int adviserUserId)
        {
            return _context.AdviserAssignments
                .AsNoTracking()
                .Where(a => a.AdviserUserId == adviserUserId)
                .Select(a => a.Student)
                .Include(s => s.Course)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();
        }
    }
}
