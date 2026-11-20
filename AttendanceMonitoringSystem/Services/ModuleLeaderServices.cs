
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Services
{
    // Retrieves module and attendance information for Module Leaders.
    public class ModuleLeaderService
    {
        private readonly ApplicationDbContext _context;

        // The database context provides access to module assignments,
        // modules, students, sessions and attendance records.
        public ModuleLeaderService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Returns the module IDs assigned to this particular Module Leader.
        private async Task<List<int>> GetAssignedModuleIdsAsync(int userId)
        {
            return await _context.ModuleLeaderAssignments
                .AsNoTracking()
                .Where(a => a.ModuleLeaderUserId == userId)
                .Select(a => a.ModuleId)
                .Distinct()
                .ToListAsync();
        }

        // Builds the information displayed on the Module Leader dashboard.
        public async Task<ModuleLeaderDashboardViewModel> GetDashboardAsync(
            int userId)
        {
            // The attendance compliance threshold defined in the SRD.
            const int warningThreshold = 75;

            var model = new ModuleLeaderDashboardViewModel
            {
                AttendanceWarningThreshold = warningThreshold
            };

            // Only retrieve modules assigned to this Module Leader.
            var assignedModuleIds =
                await GetAssignedModuleIdsAsync(userId);

            if (assignedModuleIds.Count == 0)
            {
                return model;
            }

            var modules = await _context.Modules
                .AsNoTracking()
                .Where(m => assignedModuleIds.Contains(m.ModuleId))
                .OrderBy(m => m.ModuleCode)
                .ToListAsync();

            // Process each assigned module independently.
            foreach (var module in modules)
            {
                // Retrieve students registered on the module's course.
                var students = await _context.Students
                    .AsNoTracking()
                    .Where(s => s.CourseId == module.CourseId)
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToListAsync();

                // Retrieve attendance records belonging to this module.
                // Session information is required for weekly calculations.
                var attendanceRecords = await _context.Attendances
                    .AsNoTracking()
                    .Where(a =>
                        a.Student.CourseId == module.CourseId &&
                        a.Session.ModuleOccurrence.ModuleId == module.ModuleId)
                    .Include(a => a.Session)
                    .ToListAsync();

                // Calculate the module's overall attendance percentage.
                model.Modules.Add(new ModuleLeaderModuleSummaryViewModel
                {
                    ModuleId = module.ModuleId,
                    ModuleCode = module.ModuleCode,
                    ModuleName = module.ModuleName,
                    StudentCount = students.Count,
                    AttendancePercentage =
                        CalculateAttendancePercentage(attendanceRecords)
                });

                // Build attendance information for each student.
                foreach (var student in students)
                {
                    var studentRecords = attendanceRecords
                        .Where(a => a.StudentId == student.StudentId)
                        .ToList();

                    // Identify students with two or more consecutive
                    // weeks without attendance engagement.
                    int disengagedWeeks =
                        GetConsecutiveDisengagedWeeks(studentRecords);

                    if (disengagedWeeks >= 2)
                    {
                        model.DisengagedStudents.Add(
                            CreateStudentSummary(
                                student,
                                module,
                                studentRecords,
                                disengagedWeeks));
                    }

                    // Ignore students without countable attendance records.
                    int denominator =
                        GetAttendanceDenominator(studentRecords);

                    if (denominator == 0)
                    {
                        continue;
                    }

                    // Flag students below the 75% compliance threshold.
                    double percentage =
                        CalculateAttendancePercentage(studentRecords);

                    if (percentage < warningThreshold)
                    {
                        model.LowAttendanceStudents.Add(
                            CreateStudentSummary(
                                student,
                                module,
                                studentRecords));
                    }
                }
            }

            // Show modules with the lowest attendance first.
            model.Modules = model.Modules
                .OrderBy(m => m.AttendancePercentage)
                .ThenBy(m => m.ModuleCode)
                .ToList();

            // Show the lowest-attendance students first.
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

        // Creates a student's attendance summary for a particular module.
        private static ModuleLeaderStudentSummaryViewModel CreateStudentSummary(
            Student student,
            Module module,
            List<Attendance> records,
            int disengagedWeeks = 0)
        {
            int attended = records.Count(a =>
                a.Status == AttendanceStatus.Present ||
                a.Status == AttendanceStatus.LateArrival ||
                a.Status == AttendanceStatus.LeftEarly);

            return new ModuleLeaderStudentSummaryViewModel
            {
                StudentId = student.StudentId,
                StudentName = $"{student.FirstName} {student.LastName}",
                Email = student.Email,
                ModuleCode = module.ModuleCode,
                ModuleName = module.ModuleName,
                TotalRecords = GetAttendanceDenominator(records),
                AttendedCount = attended,
                AttendancePercentage =
                    CalculateAttendancePercentage(records),
                ConsecutiveWeeksWithoutEngagement = disengagedWeeks
            };
        }

        // Authorised absences and unrecorded entries are excluded
        // from the attendance percentage denominator.
        private static int GetAttendanceDenominator(
            List<Attendance> records)
        {
            return records.Count(a =>
                a.Status == AttendanceStatus.Present ||
                a.Status == AttendanceStatus.Absent ||
                a.Status == AttendanceStatus.LateArrival ||
                a.Status == AttendanceStatus.LeftEarly);
        }

        // Present, Late Arrival and Left Early count as attended.
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

        // Counts consecutive calendar weeks where the student has
        // completed sessions but has not attended any of them.
        private static int GetConsecutiveDisengagedWeeks(
            List<Attendance> records)
        {
            var weeklyRecords = records
                .GroupBy(a =>
                {
                    // Calculate the Monday of the session's calendar week.
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
                // entries do not count towards disengagement.
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
