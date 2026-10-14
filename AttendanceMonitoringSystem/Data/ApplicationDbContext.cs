using AttendanceMonitoringSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<ModuleOccurrence> ModuleOccurrences => Set<ModuleOccurrence>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<AttendanceHistory> AttendanceHistories => Set<AttendanceHistory>();
    public DbSet<AdviserAssignment> AdviserAssignments => Set<AdviserAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // A user can have each role only once.
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Course -> Modules -> ModuleOccurrences -> Sessions.
        modelBuilder.Entity<Module>()
            .HasOne(m => m.Course)
            .WithMany(c => c.Modules)
            .HasForeignKey(m => m.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ModuleOccurrence>()
            .HasOne(mo => mo.Module)
            .WithMany(m => m.ModuleOccurrences)
            .HasForeignKey(mo => mo.ModuleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Session>()
            .HasOne(s => s.ModuleOccurrence)
            .WithMany(mo => mo.Sessions)
            .HasForeignKey(s => s.ModuleOccurrenceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Students belong to a course.
        modelBuilder.Entity<Student>()
            .HasOne(s => s.Course)
            .WithMany()
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Each student can have at most one attendance record per session.
        modelBuilder.Entity<Attendance>()
            .HasIndex(a => new { a.StudentId, a.SessionId })
            .IsUnique();

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Student)
            .WithMany(s => s.Attendances)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Session)
            .WithMany(s => s.Attendances)
            .HasForeignKey(a => a.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.RecordedByUser)
            .WithMany()
            .HasForeignKey(a => a.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Preserve attendance history and identify who changed the record.
        modelBuilder.Entity<AttendanceHistory>()
            .HasOne(h => h.Attendance)
            .WithMany()
            .HasForeignKey(h => h.AttendanceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AttendanceHistory>()
            .HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // An adviser assignment links a user to a student.
        modelBuilder.Entity<AdviserAssignment>()
            .HasOne(a => a.Adviser)
            .WithMany()
            .HasForeignKey(a => a.AdviserUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdviserAssignment>()
            .HasOne(a => a.Student)
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Store enum values as integers.
        modelBuilder.Entity<Attendance>()
            .Property(a => a.Status)
            .HasConversion<int>();

        modelBuilder.Entity<AttendanceHistory>()
            .Property(h => h.PreviousStatus)
            .HasConversion<int>();

        modelBuilder.Entity<AttendanceHistory>()
            .Property(h => h.NewStatus)
            .HasConversion<int>();

        modelBuilder.Entity<Session>()
            .Property(s => s.Status)
            .HasConversion<int>();
    }
}