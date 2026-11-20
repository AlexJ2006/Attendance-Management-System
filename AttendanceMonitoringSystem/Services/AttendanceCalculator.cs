using AttendanceMonitoringSystem.Models;

namespace AttendanceMonitoringSystem.Services;

// One set of calculation rules used by every dashboard and report
// (portfolio section 3.1: "consistent calculation rules").
//
//  - Present, Late Arrival and Left Early count as attended.
//  - Absent counts against the student.
//  - Authorised Absence and Not Recorded are left out of the calculation,
//    so authorised absences never lower a student's percentage.
public static class AttendanceCalculator
{
    public static bool IsAttended(AttendanceStatus status) =>
        status is AttendanceStatus.Present
            or AttendanceStatus.LateArrival
            or AttendanceStatus.LeftEarly;

    public static bool CountsTowardsPercentage(AttendanceStatus status) =>
        IsAttended(status) || status == AttendanceStatus.Absent;

    public static double Percentage(IEnumerable<AttendanceStatus> statuses)
    {
        var list = statuses.ToList();
        var eligible = list.Count(CountsTowardsPercentage);

        return eligible == 0
            ? 0
            : Math.Round(list.Count(IsAttended) * 100.0 / eligible, 1);
    }

    // Human-readable status names.
    public static string Display(AttendanceStatus status) => status switch
    {
        AttendanceStatus.Present => "Present",
        AttendanceStatus.Absent => "Absent",
        AttendanceStatus.AuthorisedAbsence => "Authorised Absence",
        AttendanceStatus.LateArrival => "Late Arrival",
        AttendanceStatus.LeftEarly => "Left Early",
        _ => "Not Recorded"
    };

    // A symbol shown next to every status so it can be told apart without
    // colour, e.g. in greyscale or by colour-blind users (test T14).
    public static string Symbol(AttendanceStatus status) => status switch
    {
        AttendanceStatus.Present => "✓",
        AttendanceStatus.Absent => "✗",
        AttendanceStatus.AuthorisedAbsence => "A",
        AttendanceStatus.LateArrival => "L",
        AttendanceStatus.LeftEarly => "E",
        _ => "–"
    };

    public static string CssClass(AttendanceStatus status) => status switch
    {
        AttendanceStatus.Present => "status-present",
        AttendanceStatus.Absent => "status-absent",
        AttendanceStatus.AuthorisedAbsence => "status-authorised",
        AttendanceStatus.LateArrival => "status-late",
        AttendanceStatus.LeftEarly => "status-left-early",
        _ => "status-not-recorded"
    };
}
