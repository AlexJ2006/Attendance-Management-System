using Microsoft.Extensions.Caching.Memory;

namespace AttendanceMonitoringSystem.Services;

// Limits repeated invalid attendance-code submissions from one account
// (test T15, STRIDE: denial of service and code guessing).
// Limits are tracked per user, so other students are not affected.
public class CodeAttemptLimiter
{
    public const int MaxInvalidAttempts = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    private readonly IMemoryCache _cache;
    private readonly object _lock = new();

    public CodeAttemptLimiter(IMemoryCache cache)
    {
        _cache = cache;
    }

    private static string Key(int userId) => $"code-attempts:{userId}";

    public bool IsLockedOut(int userId) =>
        _cache.TryGetValue<int>(Key(userId), out var count) &&
        count >= MaxInvalidAttempts;

    // Records an invalid attempt and returns the attempts remaining.
    public int RecordInvalidAttempt(int userId)
    {
        lock (_lock)
        {
            _cache.TryGetValue<int>(Key(userId), out var count);
            count++;
            _cache.Set(Key(userId), count, Window);
            return Math.Max(0, MaxInvalidAttempts - count);
        }
    }

    public void Reset(int userId) => _cache.Remove(Key(userId));
}
