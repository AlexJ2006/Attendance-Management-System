using AttendanceMonitoringSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Data;
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<ModuleOccurrence> ModuleOccurrences
        => Set<ModuleOccurrence>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<AttendanceHistory> AttendanceHistories
        => Set<AttendanceHistory>();
    public DbSet<AdviserAssignment> AdviserAssignments
        => Set<AdviserAssignment>();
    
    public DbSet<ModuleLeaderAssignment> ModuleLeaderAssignments
    => Set<ModuleLeaderAssignment>();

    public DbSet<CourseLeaderAssignment> CourseLeaderAssignments
    => Set<CourseLeaderAssignment>();

    // Stores which Tutors are assigned to each module occurrence.
    public DbSet<TutorAssignment> TutorAssignments
    => Set<TutorAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");
            entity.HasKey(u => u.UserId);

            entity.Property(u => u.UserId).HasColumnName("UserId");
            entity.Property(u => u.Name).HasColumnName("Name");
            entity.Property(u => u.Email).HasColumnName("Email");
            entity.Property(u => u.IsActive).HasColumnName("is_active");
            entity.Property(u => u.PasswordHash)
            .HasColumnName("PasswordHash");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");
            entity.HasKey(r => r.RoleId);

            entity.Property(r => r.RoleId).HasColumnName("RoleId");
            entity.Property(r => r.RoleName).HasColumnName("name");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRole");
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.Property(ur => ur.UserId).HasColumnName("user_id");
            entity.Property(ur => ur.RoleId).HasColumnName("role_id");
            entity.Property(ur => ur.AssignedAt).HasColumnName("AssignedAt");

            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Course");
            entity.HasKey(c => c.CourseId);

            entity.Property(c => c.CourseId).HasColumnName("CourseId");
            entity.Property(c => c.CourseName).HasColumnName("CourseName");
            entity.Property(c => c.CourseCode).HasColumnName("CourseCode");
        });

        modelBuilder.Entity<Module>(entity =>
        {
            entity.ToTable("Module");
            entity.HasKey(m => m.ModuleId);
            entity.Property(m => m.ModuleId).HasColumnName("ModuleId");
            entity.Property(m => m.CourseId).HasColumnName("CourseId");
            entity.Property(m => m.ModuleName).HasColumnName("ModuleName");
            entity.Property(m => m.ModuleCode).HasColumnName("ModuleCode");

            // The existing Module table has no YearOfStudy column.

            entity.HasOne(m => m.Course)
                .WithMany(c => c.Modules)
                .HasForeignKey(m => m.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ModuleOccurrence>(entity =>
        {
            entity.ToTable("ModuleOccurrence");
            entity.HasKey(mo => mo.ModuleOccurrenceId);

            entity.Property(mo => mo.ModuleOccurrenceId)
                .HasColumnName("OccurrenceId");

            entity.Property(mo => mo.ModuleId).HasColumnName("ModuleId");
            entity.Property(mo => mo.AcademicYear).HasColumnName("AcademicYear");
            entity.Property(mo => mo.Semester).HasColumnName("Semester");

            entity.HasOne(mo => mo.Module)
                .WithMany(m => m.ModuleOccurrences)
                .HasForeignKey(mo => mo.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Student");
            entity.HasKey(s => s.StudentId);

            entity.Property(s => s.StudentId).HasColumnName("StudentId");
            entity.Property(s => s.FirstName).HasColumnName("FirstName");
            entity.Property(s => s.LastName).HasColumnName("LastName");
            entity.Property(s => s.Email).HasColumnName("Email");
            entity.Property(s => s.CourseId).HasColumnName("CourseId");
            entity.Property(s => s.YearOfStudy).HasColumnName("YearOfStudy");
            entity.Property(s => s.Department).HasColumnName("Department");

            // Present in the database; add the matching property/model
            // before querying or saving this relationship.
            entity.Property<int>("CohortId").HasColumnName("CohortId");

            entity.HasOne(s => s.Course)
                .WithMany()
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => s.CourseId);
        });

        
        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("TeachingSession");
            entity.HasKey(s => s.SessionId);

            entity.Property(s => s.SessionId)
                .HasColumnName("SessionId");

            entity.Property(s => s.ModuleOccurrenceId)
                .HasColumnName("ModuleOccurrenceId");

            entity.Property(s => s.StartTime)
                .HasColumnName("StartTime");

            entity.Property(s => s.EndTime)
                .HasColumnName("EndTime");

            entity.Property(s => s.SessionCode)
                .HasColumnName("SessionCode");

            entity.Property(s => s.CodeExpiresAt)
                .HasColumnName("CodeExpiresAt");

            entity.Property(s => s.SessionType)
                .HasColumnName("SessionType");

            entity.Property(s => s.Status)
                .HasColumnName("Status");

            entity.Ignore(s => s.SessionDate);

            entity.HasOne(s => s.ModuleOccurrence)
                .WithMany(mo => mo.Sessions)
                .HasForeignKey(s => s.ModuleOccurrenceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.ToTable("AttendanceRecord");
            entity.HasKey(a => a.AttendanceId);

            entity.Property(a => a.AttendanceId).HasColumnName("AttendanceId");
            entity.Property(a => a.StudentId).HasColumnName("StudentId");
            entity.Property(a => a.SessionId).HasColumnName("SessionId");
            entity.Property(a => a.Status)
                .HasColumnName("Status");
            entity.Property(a => a.RecordedAt).HasColumnName("RecordedAt");
            entity.Property(a => a.RecordedByUserId)
                .HasColumnName("RecordedByUserId");

            entity.HasIndex(a => new { a.StudentId, a.SessionId })
                .IsUnique();

            entity.HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Session)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.SessionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.RecordedByUser)
                .WithMany()
                .HasForeignKey(a => a.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AttendanceHistory>(entity =>
        {
            entity.ToTable("AttendanceCorrection");
            entity.HasKey(h => h.AttendanceHistoryId);

            entity.Property(h => h.AttendanceHistoryId)
                .HasColumnName("CorrectionId");
            entity.Property(h => h.AttendanceId).HasColumnName("AttendanceId");
            entity.Property(h => h.NewStatus)
                .HasColumnName("Status");
            entity.Property(h => h.ChangedAt).HasColumnName("CorrectedAt");
            entity.Property(h => h.Reason).HasColumnName("Reason");
            entity.Property(h => h.ChangedByUserId).HasColumnName("CorrectedBy");

            // The existing database does not show a PreviousStatus column.
            entity.Ignore(h => h.PreviousStatus);

            entity.HasOne(h => h.Attendance)
                .WithMany()
                .HasForeignKey(h => h.AttendanceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.ChangedByUser)
                .WithMany()
                .HasForeignKey(h => h.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AdviserAssignment>(entity =>
        {
            entity.ToTable("AdviserAssignments");
            entity.HasKey(a => a.AdviserAssignmentId);

            entity.Property(a => a.AdviserAssignmentId)
                .HasColumnName("AdviserAssignmentId");
            entity.Property(a => a.AdviserUserId).HasColumnName("AdviserUserId");
            entity.Property(a => a.StudentId).HasColumnName("StudentId");

            entity.HasOne(a => a.Adviser)
                .WithMany()
                .HasForeignKey(a => a.AdviserUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ModuleLeaderAssignment>(entity =>
        {
            entity.ToTable("ModuleLeaderAssignments");

            entity.HasKey(a => a.ModuleLeaderAssignmentId);

            entity.Property(a => a.ModuleLeaderAssignmentId)
                .HasColumnName("ModuleLeaderAssignmentId");

            entity.Property(a => a.ModuleLeaderUserId)
                .HasColumnName("ModuleLeaderUserId");

            entity.Property(a => a.ModuleId)
                .HasColumnName("ModuleId");

            entity.HasOne(a => a.ModuleLeader)
                .WithMany()
                .HasForeignKey(a => a.ModuleLeaderUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Module)
                .WithMany()
                .HasForeignKey(a => a.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CourseLeaderAssignment>(entity =>
        {
            entity.ToTable("CourseLeaderAssignments");

            entity.HasKey(a => a.CourseLeaderAssignmentId);

            entity.Property(a => a.CourseLeaderAssignmentId)
                .HasColumnName("CourseLeaderAssignmentId");

            entity.Property(a => a.CourseLeaderUserId)
                .HasColumnName("CourseLeaderUserId");

            entity.Property(a => a.CourseId)
                .HasColumnName("CourseId");

            entity.HasOne(a => a.CourseLeader)
                .WithMany()
                .HasForeignKey(a => a.CourseLeaderUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Course)
                .WithMany()
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        // Configure the Tutor-to-module-occurrence assignment table.
        modelBuilder.Entity<TutorAssignment>(entity =>
        {
            entity.ToTable("TutorAssignments");

            entity.HasKey(a => a.TutorAssignmentId);

            entity.Property(a => a.TutorAssignmentId)
                .HasColumnName("TutorAssignmentId");

            entity.Property(a => a.TutorUserId)
                .HasColumnName("TutorUserId");

            entity.Property(a => a.ModuleOccurrenceId)
                .HasColumnName("ModuleOccurrenceId");

            // Prevent the same Tutor being assigned to the same occurrence twice.
            entity.HasIndex(a => new
                {
                    a.TutorUserId,
                    a.ModuleOccurrenceId
                })
                .IsUnique();

            // Link the assignment to an existing Tutor user account.
            entity.HasOne(a => a.Tutor)
                .WithMany()
                .HasForeignKey(a => a.TutorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Link the assignment to an existing module occurrence.
            entity.HasOne(a => a.ModuleOccurrence)
                .WithMany()
                .HasForeignKey(a => a.ModuleOccurrenceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

