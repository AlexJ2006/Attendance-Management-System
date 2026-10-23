
using AttendanceMonitoringSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;

namespace AttendanceMonitoringSystem.Data;

public static class PasswordSetup
{
    public static async Task ResetPasswordAsync(
        string connectionString,
        string email,
        string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Password cannot be empty.");
        }

        await using var connection =
            new SqliteConnection(connectionString);

        await connection.OpenAsync();

        const string sql = """
            SELECT UserId, Name, Email
            FROM "User"
            WHERE Email = $email
              AND is_active = 1
            LIMIT 1;
            """;

        int userId;
        string name;
        string userEmail;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            command.Parameters.AddWithValue("$email", email.Trim());

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException(
                    "No active account was found for that email.");
            }

            userId = reader.GetInt32(0);
            name = reader.GetString(1);
            userEmail = reader.GetString(2);
        }

        var user = new User
        {
            UserId = userId,
            Name = name,
            Email = userEmail
        };

        var hasher = new PasswordHasher<User>();
        var hash = hasher.HashPassword(user, password);

        await using var update = connection.CreateCommand();

        update.CommandText = """
            UPDATE "User"
            SET PasswordHash = $hash
            WHERE UserId = $id;
            """;

        update.Parameters.AddWithValue("$hash", hash);
        update.Parameters.AddWithValue("$id", userId);

        if (await update.ExecuteNonQueryAsync() != 1)
        {
            throw new InvalidOperationException(
                "Password reset did not update exactly one account.");
        }

        Console.WriteLine($"Password set successfully for {userEmail}.");
    }
}
