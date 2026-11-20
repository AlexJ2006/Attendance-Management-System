using AttendanceMonitoringSystem.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AttendanceMonitoringSystem.Data;

// Brings an existing attendance.db up to the schema the application expects.
// Every step checks before it changes anything, so it is safe to run on
// every start-up and on databases that have already been upgraded.
public static class DatabaseInitialiser
{
    // The six roles described in the SAAD portfolio (section 3.1).
    public static readonly string[] RequiredRoles =
    {
        RoleNames.Student,
        RoleNames.Tutor,
        RoleNames.ModuleLeader,
        RoleNames.CourseLeader,
        RoleNames.Adviser,
        RoleNames.Administrator
    };

    public static async Task InitialiseAsync(
        ApplicationDbContext context,
        ILogger logger)
    {
        var connection = (SqliteConnection)context.Database.GetDbConnection();
        await connection.OpenAsync();

        // Enforce foreign keys (SQLite leaves them off by default) and use
        // write-ahead logging so reads are not blocked during the 9am peak.
        await ExecuteAsync(connection, "PRAGMA foreign_keys = ON;");
        await ExecuteAsync(connection, "PRAGMA journal_mode = WAL;");

        // Columns added after the original database was created.
        await AddColumnIfMissingAsync(connection, "User",
            "FailedLoginAttempts", "INTEGER NOT NULL DEFAULT 0", logger);
        await AddColumnIfMissingAsync(connection, "User",
            "LockoutEndUtc", "TEXT NULL", logger);
        await AddColumnIfMissingAsync(connection, "AttendanceCorrection",
            "PreviousStatus", "INTEGER NOT NULL DEFAULT 0", logger);

        // Indexes that support the unique constraints in section 3.5.2
        // and the queries used by the dashboards and reports.
        await ExecuteAsync(connection,
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_User_Email ON \"User\" (Email);");
        await ExecuteAsync(connection,
            "CREATE INDEX IF NOT EXISTS IX_Student_UserId ON Student (UserId);");
        await ExecuteAsync(connection,
            "CREATE INDEX IF NOT EXISTS IX_TeachingSession_SessionCode ON TeachingSession (SessionCode);");
        await ExecuteAsync(connection,
            "CREATE INDEX IF NOT EXISTS IX_TeachingSession_StartTime ON TeachingSession (StartTime);");
        await ExecuteAsync(connection,
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_ModuleLeaderAssignments_Unique ON ModuleLeaderAssignments (ModuleLeaderUserId, ModuleId);");
        await ExecuteAsync(connection,
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_CourseLeaderAssignments_Unique ON CourseLeaderAssignments (CourseLeaderUserId, CourseId);");

        // The original database used "Lecturer"; the portfolio calls this role "Tutor".
        await ExecuteAsync(connection,
            "UPDATE Role SET name = 'Tutor' WHERE name = 'Lecturer' " +
            "AND NOT EXISTS (SELECT 1 FROM Role WHERE name = 'Tutor');");

        foreach (var roleName in RequiredRoles)
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                "INSERT INTO Role (name) SELECT $name " +
                "WHERE NOT EXISTS (SELECT 1 FROM Role WHERE name = $name);";
            command.Parameters.AddWithValue("$name", roleName);

            if (await command.ExecuteNonQueryAsync() > 0)
            {
                logger.LogInformation("Added missing role {Role}.", roleName);
            }
        }
    }

    private static async Task AddColumnIfMissingAsync(
        SqliteConnection connection,
        string table,
        string column,
        string definition,
        ILogger logger)
    {
        await using var check = connection.CreateCommand();
        check.CommandText =
            $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = $column;";
        check.Parameters.AddWithValue("$column", column);

        var exists = Convert.ToInt32(await check.ExecuteScalarAsync()) > 0;

        if (!exists)
        {
            await ExecuteAsync(connection,
                $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition};");

            logger.LogInformation(
                "Added column {Table}.{Column}.", table, column);
        }
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
