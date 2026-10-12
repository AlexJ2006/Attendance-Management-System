
using AttendanceMonitoringSystem.Models;

namespace AttendanceMonitoringSystem.Services
{
    public class AttendanceService
    {
        private static readonly List<Attendance> AttendanceRecords = new();
        private static readonly List<AttendanceHistory> AttendanceHistoryRecords = new();

        private static int _nextAttendanceId = 1;
        private static int _nextHistoryId = 1;

        public List<Attendance> GetAllAttendance()
        {
            return AttendanceRecords.ToList();
        }

        public Attendance? GetAttendanceById(int attendanceId)
        {
            return AttendanceRecords.FirstOrDefault(
                a => a.AttendanceId == attendanceId);
        }

        public bool RecordAttendance(Attendance attendance)
        {
            ArgumentNullException.ThrowIfNull(attendance);

            if (attendance.StudentId <= 0 || attendance.SessionId <= 0)
            {
                return false;
            }

            // Prevent duplicate attendance records for the same student and session.
            bool alreadyExists = AttendanceRecords.Any(a =>
                a.StudentId == attendance.StudentId &&
                a.SessionId == attendance.SessionId);

            if (alreadyExists)
            {
                return false;
            }

            if (attendance.AttendanceId <= 0)
            {
                attendance.AttendanceId = _nextAttendanceId++;
            }
            else
            {
                if (AttendanceRecords.Any(a =>
                    a.AttendanceId == attendance.AttendanceId))
                {
                    return false;
                }

                _nextAttendanceId = Math.Max(
                    _nextAttendanceId,
                    attendance.AttendanceId + 1);
            }

            if (attendance.RecordedAt == default)
            {
                attendance.RecordedAt = DateTime.UtcNow;
            }

            AttendanceRecords.Add(attendance);
            return true;
        }

        public bool CorrectAttendance(
            int attendanceId,
            AttendanceStatus newStatus,
            int changedByUserId,
            string reason)
        {
            var attendance = GetAttendanceById(attendanceId);

            if (attendance == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(reason) || changedByUserId <= 0)
            {
                return false;
            }

            if (!Enum.IsDefined(typeof(AttendanceStatus), newStatus))
            {
                return false;
            }

            if (attendance.Status == newStatus)
            {
                return false;
            }

            var history = new AttendanceHistory
            {
                AttendanceHistoryId = _nextHistoryId++,
                AttendanceId = attendance.AttendanceId,
                PreviousStatus = attendance.Status,
                NewStatus = newStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = changedByUserId,
                Reason = reason.Trim()
            };

            // Record the change before updating the current status.
            AttendanceHistoryRecords.Add(history);

            attendance.Status = newStatus;

            return true;
        }

        public List<AttendanceHistory> GetAttendanceHistory(int attendanceId)
        {
            return AttendanceHistoryRecords
                .Where(h => h.AttendanceId == attendanceId)
                .OrderBy(h => h.ChangedAt)
                .ToList();
        }
    }
}
