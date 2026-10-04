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
        Course = "Software Engineering",
        YearOfStudy = 3,
        Department = "Engineering",
      },
      new Student
      {
        StudentId = 5678,
        FirstName = "Michael",
        LastName = "Smithson",
        Email = "M.Smithson@EmailProvider.com",
        Course = "Mathematics",
        YearOfStudy = 2,
        Department = "Mathematics"
      }
    };

    public class StudentController : Controller
    {
        public IActionResult Index()
        {
            return View(Students);
        }
    }
    
    public IActionResult EnrolmentDetails(int StudentId)
    {
        // Finding the student 
        foreach (Student s in Students)
        {
            if (s.StudentId == StudentId)
            {
                // Student found
                return View(s);
            }
        }

        // Student not found
        return NotFound();
    }

    // public IActionResult EditCourse(string Course)
    // {
    // }
}
}