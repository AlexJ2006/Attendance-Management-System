
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Services
{
    public class CourseLeaderService
    {
        private readonly ApplicationDbContext _context;

        // The database context provides access to courses, assignments,
        // students, modules, sessions and attendance records.
        public CourseLeaderService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Returns the courses assigned to this particular Course Leader.
        private async Task<List<int>> GetAssignedCourseIdsAsync(int userId)
        {
            return await _context.CourseLeaderAssignments
                .AsNoTracking()
                .Where(a => a.CourseLeaderUserId == userId)
                .Select(a => a.CourseId)
                .Distinct()
                .ToListAsync();
        }

        // Builds the information displayed on the Course Leader dashboard.
        public async Task<CourseLeaderDashboardViewModel> GetDashboardAsync(
            int userId)
        {
            // The SRD defines attendance below 75% as non-compliant.
            const int warningThreshold = 75;

            var model = new CourseLeaderDashboardViewModel
            {
                AttendanceWarningThreshold = warningThreshold
            };

            var assignedCourseIds = await GetAssignedCourseIdsAsync(userId);

            // An empty assignment list means the user has no courses to view.
            if (assignedCourseIds.Count == 0)
            {
                return model;
            }

            // Only retrieve courses linked to this Course Leader.
            var courses = await _context.Courses
                .AsNoTracking()
                .Where(c => assignedCourseIds.Contains(c.CourseId))
                .Include(c => c.Modules)
                .OrderBy(c => c.CourseCode)
                .ToListAsync();

            foreach (var course in courses)
            {
                var students = await _context.Students
                    .AsNoTracking()
                    .Where(s => s.CourseId == course.CourseId)
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToListAsync();

                // Retrieve attendance records and session dates for
                // the disengagement calculation.
                var attendanceRecords = await _context.Attendances
                    .AsNoTracking()
                    .Where(a =>
                        a.Student.CourseId == course.CourseId &&
                        a.Session.ModuleOccurrence.Module.CourseId
                            == course.CourseId)
                    .Include(a => a.Session)
                    .ToListAsync();

                model.Courses.Add(new CourseLeaderCourseSummaryViewModel
                {
                    CourseId = course.CourseId,
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName,
                    ModuleCount = course.Modules.Count,
                    StudentCount = students.Count,
                    AttendancePercentage =
                        CalculateAttendancePercentage(attendanceRecords)
                });

                // Build attendance and disengagement information for each student.
                foreach (var student in students)
                {
                    var studentRecords = attendanceRecords
                        .Where(a => a.StudentId == student.StudentId)
                        .ToList();

                    // Check for two or more consecutive weeks without engagement.
                    int disengagedWeeks =
                        GetConsecutiveDisengagedWeeks(studentRecords);

                    if (disengagedWeeks >= 2)
                    {
                        var disengagedStudent =
                            CreateStudentSummary(student, course, studentRecords);

                        disengagedStudent.ConsecutiveWeeksWithoutEngagement =
                            disengagedWeeks;

                        model.DisengagedStudents.Add(disengagedStudent);
                    }

                    // Students without countable attendance records are not
                    // labelled as low-attendance students.
                    int denominator = GetAttendanceDenominator(studentRecords);

                    if (denominator == 0)
                    {
                        continue;
                    }

                    var percentage =
                        CalculateAttendancePercentage(studentRecords);

                    if (percentage < warningThreshold)
                    {
                        model.LowAttendanceStudents.Add(
                            CreateStudentSummary(student, course, studentRecords));
                    }
                }
            }

            // Show students with the lowest attendance first.
            model.LowAttendanceStudents = model.LowAttendanceStudents
                .OrderBy(s => s.AttendancePercentage)
                .ThenBy(s => s.StudentName)
                .ToList();

            // Show the longest disengagement periods first.
            model.DisengagedStudents = model.DisengagedStudents
                .OrderByDescending(s => s.ConsecutiveWeeksWithoutEngagement)
                .ThenBy(s => s.StudentName)
                .ToList();

            return model;
        }

        // Shows modules, students and attendance for one assigned course.
        public async Task<CourseLeaderCourseViewModel?> GetCourseAsync(
            int userId,
            int courseId)
        {
            var assignedCourseIds = await GetAssignedCourseIdsAsync(userId);

            // Check assignment before retrieving any course information.
            if (!assignedCourseIds.Contains(courseId))
            {
                return null;
            }

            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Modules)
                .FirstOrDefaultAsync(c => c.CourseId == courseId);

            if (course == null)
            {
                return null;
            }

            var students = await _context.Students
                .AsNoTracking()
                .Where(s => s.CourseId == courseId)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            var attendanceRecords = await _context.Attendances
                .AsNoTracking()
                .Where(a =>
                    a.Student.CourseId == courseId &&
                    a.Session.ModuleOccurrence.Module.CourseId == courseId)
                .ToListAsync();

            var model = new CourseLeaderCourseViewModel
            {
                Course = course,
                Modules = course.Modules
                    .OrderBy(m => m.ModuleCode)
                    .ToList(),
                AttendancePercentage =
                    CalculateAttendancePercentage(attendanceRecords)
            };

            foreach (var student in students)
            {
                var studentRecords = attendanceRecords
                    .Where(a => a.StudentId == student.StudentId)
                    .ToList();

                model.Students.Add(
                    CreateStudentSummary(student, course, studentRecords));
            }

            return model;
        }

        // Retrieves monitoring information for one module.
        // The module must belong to a course assigned to this Course Leader.
        public async Task<CourseLeaderModuleViewModel?> GetModuleAsync(
            int userId,
            int moduleId)
        {
            var assignedCourseIds = await GetAssignedCourseIdsAsync(userId);

            // Retrieve the module only if its course is assigned to this user.
            var module = await _context.Modules
                .AsNoTracking()
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m =>
                    m.ModuleId == moduleId &&
                    assignedCourseIds.Contains(m.CourseId));

            // Do not reveal information about modules outside the user's scope.
            if (module == null)
            {
                return null;
            }

            // Retrieve students enrolled on the module's course.
            var students = await _context.Students
                .AsNoTracking()
                .Where(s => s.CourseId == module.CourseId)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            // Only include attendance for this module.
            // Both the student's course and module's course are checked.
            var attendanceRecords = await _context.Attendances
                .AsNoTracking()
                .Where(a =>
                    a.Student.CourseId == module.CourseId &&
                    a.Session.ModuleOccurrence.Module.CourseId == module.CourseId &&
                    a.Session.ModuleOccurrence.ModuleId == moduleId)
                .ToListAsync();

            var model = new CourseLeaderModuleViewModel
            {
                Module = module,
                AttendancePercentage =
                    CalculateAttendancePercentage(attendanceRecords)
            };

            // Build an attendance summary for each student on the course.
            foreach (var student in students)
            {
                var studentRecords = attendanceRecords
                    .Where(a => a.StudentId == student.StudentId)
                    .ToList();

                model.Students.Add(
                    CreateStudentSummary(
                        student,
                        module.Course,
                        studentRecords));
            }

            return model;
        }

        // Returns attendance history only if the student's course is assigned
        // to the signed-in Course Leader.
        public async Task<CourseLeaderStudentAttendanceViewModel?>
            GetStudentAttendanceAsync(int userId, int studentId)
        {
            var assignedCourseIds = await GetAssignedCourseIdsAsync(userId);

            var student = await _context.Students
                .AsNoTracking()
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s =>
                    s.StudentId == studentId &&
                    assignedCourseIds.Contains(s.CourseId));

            if (student == null)
            {
                return null;
            }

            var records = await _context.Attendances
                .AsNoTracking()
                .Where(a =>
                    a.StudentId == studentId &&
                    a.Student.CourseId == student.CourseId &&
                    a.Session.ModuleOccurrence.Module.CourseId
                        == student.CourseId)
                .Include(a => a.Session)
                    .ThenInclude(s => s.ModuleOccurrence)
                        .ThenInclude(o => o.Module)
                .OrderByDescending(a => a.Session.StartTime)
                .ToListAsync();

            return new CourseLeaderStudentAttendanceViewModel
            {
                Student = student,
                AttendanceRecords = records,
                AttendancePercentage =
                    CalculateAttendancePercentage(records)
            };
        }

        // Creates one student's summary for use on the dashboard,
        // course page and module page.
        private static CourseLeaderStudentSummaryViewModel CreateStudentSummary(
            Student student,
            Course course,
            List<Attendance> records)
        {
            int attended = records.Count(a =>
                a.Status == AttendanceStatus.Present ||
                a.Status == AttendanceStatus.LateArrival ||
                a.Status == AttendanceStatus.LeftEarly);

            return new CourseLeaderStudentSummaryViewModel
            {
                StudentId = student.StudentId,
                StudentName = $"{student.FirstName} {student.LastName}",
                Email = student.Email,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                TotalRecords = records.Count,
                AttendedCount = attended,
                AttendancePercentage =
                    CalculateAttendancePercentage(records)
            };
        }

        // Authorised absences and unrecorded entries are excluded from
        // the attendance percentage denominator.
        private static int GetAttendanceDenominator(
            List<Attendance> records)
        {
            return records.Count(a =>
                a.Status == AttendanceStatus.Present ||
                a.Status == AttendanceStatus.Absent ||
                a.Status == AttendanceStatus.LateArrival ||
                a.Status == AttendanceStatus.LeftEarly);
        }

        // Present, late arrivals and leaving early count as attended.
        private static double CalculateAttendancePercentage(
            List<Attendance> records)
        {
            int denominator = GetAttendanceDenominator(records);

            if (denominator == 0)
            {
                return 0;
            }

            int attended = records.Count(a =>
                a.Status == AttendanceStatus.Present ||
                a.Status == AttendanceStatus.LateArrival ||
                a.Status == AttendanceStatus.LeftEarly);

            return Math.Round(
                (double)attended / denominator * 100, 1);
        }

        // Counts consecutive calendar teaching weeks where the student
        // has completed sessions but has not attended any of them.
        //
        // Present, Late Arrival and Left Early count as engagement.
        // Authorised Absence and Not Recorded do not count as engagement.
        private static int GetConsecutiveDisengagedWeeks(
            List<Attendance> records)
        {
            var weeklyRecords = records
                .GroupBy(a =>
                {
                    // Calculate the Monday of the session's teaching week.
                    DateTime sessionDate = a.Session.StartTime.Date;

                    int daysSinceMonday =
                        ((int)sessionDate.DayOfWeek + 6) % 7;

                    return sessionDate.AddDays(-daysSinceMonday);
                })
                .OrderBy(g => g.Key)
                .ToList();

            int consecutiveWeeks = 0;
            DateTime? previousWeek = null;

            foreach (var week in weeklyRecords)
            {
                // A gap in calendar weeks breaks the consecutive sequence.
                if (previousWeek.HasValue &&
                    (week.Key - previousWeek.Value).TotalDays != 7)
                {
                    consecutiveWeeks = 0;
                }

                previousWeek = week.Key;

                bool hasEligibleSession = week.Any(a =>
                    a.Status == AttendanceStatus.Present ||
                    a.Status == AttendanceStatus.Absent ||
                    a.Status == AttendanceStatus.LateArrival ||
                    a.Status == AttendanceStatus.LeftEarly);

                bool hasEngagement = week.Any(a =>
                    a.Status == AttendanceStatus.Present ||
                    a.Status == AttendanceStatus.LateArrival ||
                    a.Status == AttendanceStatus.LeftEarly);

                // Weeks containing only authorised absences or unrecorded
                // entries are neutral and do not trigger disengagement.
                if (!hasEligibleSession)
                {
                    consecutiveWeeks = 0;
                }
                else if (hasEngagement)
                {
                    consecutiveWeeks = 0;
                }
                else
                {
                    consecutiveWeeks++;
                }
            }

            return consecutiveWeeks;
        }
    }
}
