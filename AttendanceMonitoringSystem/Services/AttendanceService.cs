using AttendanceMonitoringSystem.Models;

namespace AttendanceMonitoringSystem.Services
{
    public class AttendanceService
    {
        private static readonly List<Attendance> AttendanceRecords = new();

        private static readonly List<AttendanceHistory> AttendanceHistoryRecords = new();

        public List<Attendance> GetAllAttendance()
        {
            return AttendanceRecords;
        }

        public Attendance? GetAttendanceById(int attendanceId)
        {
            return AttendanceRecords.FirstOrDefault(
                a => a.AttendanceId == attendanceId);
        }

        public void RecordAttendance(Attendance attendance)
        {
            AttendanceRecords.Add(attendance);
        }

        public bool CorrectAttendance(
            int attendanceId,
            string newStatus,
            int changedByUserId,
            string reason)
        {
            var attendance = GetAttendanceById(attendanceId);

            if (attendance == null)
            {
                return false;
            }

            var history = new AttendanceHistory
            {
                AttendanceHistoryId = AttendanceHistoryRecords.Count + 1,
                AttendanceId = attendance.AttendanceId,
                PreviousStatus = attendance.Status,
                NewStatus = newStatus,
                ChangedAt = DateTime.Now,
                ChangedByUserId = changedByUserId,
                Reason = reason
            };

            AttendanceHistoryRecords.Add(history);

            attendance.Status = newStatus;
            attendance.RecordedAt = DateTime.Now;

            return true;
        }

        public List<AttendanceHistory> GetAttendanceHistory(int attendanceId)
        {
            return AttendanceHistoryRecords
                .Where(h => h.AttendanceId == attendanceId)
                .ToList();
        }
    }
}