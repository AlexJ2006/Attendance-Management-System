using AttendanceMonitoringSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceMonitoringSystem.Controllers
{
public class StudentsController : Controller
{
    private static List<Student> Students = new()
    {
      new Student
      {
        StudentId = 1234,
        FirstName = "Geoff",
        LastName = "Briggs",
        Email = "G.Briggs@emailProvider.com",
        YearOfStudy = 3,
        Department = "Engineering"
      },
      new Student
      {
        StudentId = 5678,
        FirstName = "Michael",
        LastName = "Smithson",
        Email = "M.Smithson@EmailProvider.com",
        YearOfStudy = 2,
        Department = "Mathematics"
      }
    };

    public class StudentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
}