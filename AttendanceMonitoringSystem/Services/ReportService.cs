using System.Globalization;
using System.Text;
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Services;

// UC05 - Generate attendance report (tests T09 and T10).
// Attendance percentages are calculated when the report is requested (ADR06).
public class ReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Modules the user may report on, based on their academic scope.
    public async Task<List<Module>> GetPermittedModulesAsync(AccessScope scope)
    {
        var query = _context.Modules.AsNoTracking();

        if (!scope.All)
        {
            var occurrenceIds = scope.OccurrenceIds.ToList();
            var studentIds = scope.StudentIds.ToList();

            query = query.Where(m =>
                m.ModuleOccurrences.Any(mo => occurrenceIds.Contains(mo.ModuleOccurrenceId)) ||
                _context.Students.Any(s => studentIds.Contains(s.StudentId) && s.CourseId == m.CourseId));
        }

        return await query.OrderBy(m => m.ModuleCode).ToListAsync();
    }

    // Students the user may report on for the selected module.
    public async Task<List<Student>> GetPermittedStudentsAsync(AccessScope scope, int? moduleId)
    {
        var query = _context.Students.AsNoTracking();

        if (moduleId.HasValue)
        {
            var courseId = await _context.Modules
                .Where(m => m.ModuleId == moduleId.Value)
                .Select(m => (int?)m.CourseId)
                .FirstOrDefaultAsync();

            query = query.Where(s => s.CourseId == courseId);
        }

        if (!scope.All)
        {
            var occurrenceIds = scope.OccurrenceIds.ToList();
            var studentIds = scope.StudentIds.ToList();

            query = query.Where(s =>
                studentIds.Contains(s.StudentId) ||
                _context.ModuleOccurrences.Any(mo =>
                    occurrenceIds.Contains(mo.ModuleOccurrenceId) &&
                    mo.Module.CourseId == s.CourseId));
        }

        return await query
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();
    }

    // Validates the filters. Returns an error message or null when valid.
    public static string? Validate(ReportFilter filter)
    {
        // Figure 3.2.3b - missing required filters.
        var missing = new List<string>();
        if (!filter.ModuleId.HasValue) missing.Add("Module");
        if (!filter.From.HasValue) missing.Add("From date");
        if (!filter.To.HasValue) missing.Add("To date");
        if (string.IsNullOrWhiteSpace(filter.Student)) missing.Add("Student");

        if (missing.Count > 0)
        {
            return "Please complete all required filters: " +
                   string.Join(", ", missing) + ".";
        }

        // Figure 3.2.3c - invalid date range.
        if (filter.To!.Value.Date < filter.From!.Value.Date)
        {
            return "Invalid date range: the 'To' date must be on or after the 'From' date.";
        }

        if (filter.Student != ReportFilter.AllStudents &&
            !int.TryParse(filter.Student, out _))
        {
            return "Please select a valid student or 'All Students'.";
        }

        return null;
    }

    public async Task<AttendanceReportResult> GenerateAsync(ReportFilter filter, AccessScope scope)
    {
        var from = filter.From!.Value.Date;
        var toExclusive = filter.To!.Value.Date.AddDays(1);
        var moduleId = filter.ModuleId!.Value;

        var query = AccessScopeService.ApplyScope(_context.Attendances.AsNoTracking(), scope)
            .Where(a =>
                a.Session.ModuleOccurrence.ModuleId == moduleId &&
                a.Session.StartTime >= from &&
                a.Session.StartTime < toExclusive);

        if (filter.Student != ReportFilter.AllStudents)
        {
            var studentId = int.Parse(filter.Student!);
            query = query.Where(a => a.StudentId == studentId);
        }

        var rows = await query
            .OrderBy(a => a.Session.StartTime)
            .ThenBy(a => a.Student.LastName)
            .ThenBy(a => a.Student.FirstName)
            .Select(a => new AttendanceReportRow
            {
                SessionDate = a.Session.StartTime,
                SessionType = a.Session.SessionType,
                ModuleCode = a.Session.ModuleOccurrence.Module.ModuleCode,
                ModuleName = a.Session.ModuleOccurrence.Module.ModuleName,
                StudentId = a.StudentId,
                StudentName = a.Student.FirstName + " " + a.Student.LastName,
                StudentEmail = a.Student.Email,
                Status = a.Status
            })
            .ToListAsync();

        var result = new AttendanceReportResult { Rows = rows };

        result.Students = rows
            .GroupBy(r => new { r.StudentId, r.StudentName, r.StudentEmail })
            .Select(g => new AttendanceReportStudentSummary
            {
                StudentId = g.Key.StudentId,
                StudentName = g.Key.StudentName,
                StudentEmail = g.Key.StudentEmail,
                Present = g.Count(r => r.Status == AttendanceStatus.Present),
                Late = g.Count(r => r.Status == AttendanceStatus.LateArrival),
                LeftEarly = g.Count(r => r.Status == AttendanceStatus.LeftEarly),
                Absent = g.Count(r => r.Status == AttendanceStatus.Absent),
                Authorised = g.Count(r => r.Status == AttendanceStatus.AuthorisedAbsence),
                Percentage = AttendanceCalculator.Percentage(g.Select(r => r.Status))
            })
            .OrderBy(s => s.StudentName)
            .ToList();

        result.OverallPercentage = AttendanceCalculator.Percentage(rows.Select(r => r.Status));

        return result;
    }

    // Builds the CSV file. Values are quoted and spreadsheet formulas are
    // neutralised so the file cannot run commands when opened (CSV injection).
    public static byte[] ToCsv(AttendanceReportResult report)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Session Date,Session Type,Module Code,Module Name,Student ID,Student Name,Student Email,Status");

        foreach (var row in report.Rows)
        {
            csv.AppendLine(string.Join(",",
                Cell(row.SessionDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)),
                Cell(row.SessionType),
                Cell(row.ModuleCode),
                Cell(row.ModuleName),
                Cell(row.StudentId.ToString(CultureInfo.InvariantCulture)),
                Cell(row.StudentName),
                Cell(row.StudentEmail),
                Cell(AttendanceCalculator.Display(row.Status))));
        }

        csv.AppendLine();
        csv.AppendLine("Student ID,Student Name,Present,Late Arrival,Left Early,Absent,Authorised Absence,Attendance %");

        foreach (var s in report.Students)
        {
            csv.AppendLine(string.Join(",",
                Cell(s.StudentId.ToString(CultureInfo.InvariantCulture)),
                Cell(s.StudentName),
                s.Present, s.Late, s.LeftEarly, s.Absent, s.Authorised,
                s.Percentage.ToString("0.0", CultureInfo.InvariantCulture)));
        }

        // UTF-8 with BOM so Excel shows names correctly.
        return Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(csv.ToString()))
            .ToArray();
    }

    private static string Cell(string? value)
    {
        value ??= string.Empty;

        if (value.Length > 0 && "=+-@\t\r".Contains(value[0]))
        {
            value = "'" + value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}

public class ReportFilter
{
    public const string AllStudents = "all";

    public int? ModuleId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    // Either a student ID or "all".
    public string? Student { get; set; }
}

public class AttendanceReportRow
{
    public DateTime SessionDate { get; set; }
    public string SessionType { get; set; } = "";
    public string ModuleCode { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public string StudentEmail { get; set; } = "";
    public AttendanceStatus Status { get; set; }
}

public class AttendanceReportStudentSummary
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public string StudentEmail { get; set; } = "";
    public int Present { get; set; }
    public int Late { get; set; }
    public int LeftEarly { get; set; }
    public int Absent { get; set; }
    public int Authorised { get; set; }
    public double Percentage { get; set; }
}

public class AttendanceReportResult
{
    public List<AttendanceReportRow> Rows { get; set; } = new();
    public List<AttendanceReportStudentSummary> Students { get; set; } = new();
    public double OverallPercentage { get; set; }
}
