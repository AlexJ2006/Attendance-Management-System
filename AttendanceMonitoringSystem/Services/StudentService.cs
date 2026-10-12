
using AttendanceMonitoringSystem.Models;

namespace AttendanceMonitoringSystem.Services
{
    public class StudentService
    {
        private static readonly List<Student> Students = new()
        {
            new Student
            {
                StudentId = 1234,
                FirstName = "Geoff",
                LastName = "Briggs",
                Email = "G.Briggs@emailProvider.com",
                CourseId = 1,
                YearOfStudy = 3,
                Department = "Engineering"
            },

            new Student
            {
                StudentId = 5678,
                FirstName = "Michael",
                LastName = "Smithson",
                Email = "M.Smithson@EmailProvider.com",
                CourseId = 2,
                YearOfStudy = 2,
                Department = "Mathematics"
            }
        };

        public List<Student> GetAllStudents()
        {
            return Students.ToList();
        }

        public Student? GetStudentById(int studentId)
        {
            return Students.FirstOrDefault(
                s => s.StudentId == studentId);
        }
    }
}
