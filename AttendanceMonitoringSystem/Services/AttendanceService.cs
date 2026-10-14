
using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Services
{
    public class AttendanceService
    {
        private readonly ApplicationDbContext _context;

        public AttendanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Attendance> GetAllAttendance()
        {
            return _context.Attendances
                .AsNoTracking()
                .Include(a => a.Student)
                .Include(a => a.Session)
                .OrderBy(a => a.SessionId)
                .ThenBy(a => a.StudentId)
                .ToList();
        }

        public Attendance? GetAttendanceById(int attendanceId)
        {
            return _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Session)
                .FirstOrDefault(a => a.AttendanceId == attendanceId);
        }

        public bool RecordAttendance(Attendance attendance)
        {
            ArgumentNullException.ThrowIfNull(attendance);

            if (attendance.StudentId <= 0 || attendance.SessionId <= 0)
            {
                return false;
            }

            if (!Enum.IsDefined(typeof(AttendanceStatus), attendance.Status))
            {
                return false;
            }

            bool alreadyExists = _context.Attendances.Any(a =>
                a.StudentId == attendance.StudentId &&
                a.SessionId == attendance.SessionId);

            if (alreadyExists)
            {
                return false;
            }

            // Confirm the referenced records exist.
            bool studentExists = _context.Students
                .Any(s => s.StudentId == attendance.StudentId);

            bool sessionExists = _context.Sessions
                .Any(s => s.SessionId == attendance.SessionId);

            bool userExists = _context.Users
                .Any(u => u.UserId == attendance.RecordedByUserId);

            if (!studentExists || !sessionExists || !userExists)
            {
                return false;
            }

            if (attendance.RecordedAt == default)
            {
                attendance.RecordedAt = DateTime.UtcNow;
            }

            // Let the database generate the primary key.
            attendance.AttendanceId = 0;

            _context.Attendances.Add(attendance);

            try
            {
                _context.SaveChanges();
                return true;
            }
            catch (DbUpdateException)
            {
                // Also handle a duplicate caused by a concurrent request.
                _context.Entry(attendance).State = EntityState.Detached;
                return false;
            }
        }

        public bool CorrectAttendance(
            int attendanceId,
            AttendanceStatus newStatus,
            int changedByUserId,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(reason) ||
                changedByUserId <= 0 ||
                !Enum.IsDefined(typeof(AttendanceStatus), newStatus))
            {
                return false;
            }

            using var transaction = _context.Database.BeginTransaction();

            var attendance = _context.Attendances
                .FirstOrDefault(a => a.AttendanceId == attendanceId);

            if (attendance == null || attendance.Status == newStatus)
            {
                return false;
            }

            bool userExists = _context.Users
                .Any(u => u.UserId == changedByUserId);

            if (!userExists)
            {
                return false;
            }

            var history = new AttendanceHistory
            {
                AttendanceId = attendance.AttendanceId,
                PreviousStatus = attendance.Status,
                NewStatus = newStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = changedByUserId,
                Reason = reason.Trim()
            };

            // Save the audit record and status change together.
            _context.AttendanceHistories.Add(history);
            attendance.Status = newStatus;

            _context.SaveChanges();
            transaction.Commit();

            return true;
        }

        public List<AttendanceHistory> GetAttendanceHistory(int attendanceId)
        {
            return _context.AttendanceHistories
                .AsNoTracking()
                .Where(h => h.AttendanceId == attendanceId)
                .Include(h => h.ChangedByUser)
                .OrderBy(h => h.ChangedAt)
                .ToList();
        }
    }
}
