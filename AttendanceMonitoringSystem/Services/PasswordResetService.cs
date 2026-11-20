
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace AttendanceMonitoringSystem.Services;

public class PasswordResetService
{
    private readonly IMemoryCache _cache;

    public PasswordResetService(IMemoryCache cache)
    {
        _cache = cache;
    }

    private static string CacheKey(string email) =>
        $"password-reset:{email.Trim().ToLowerInvariant()}";

    public string CreateCode(string email)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000)
            .ToString();

        var codeHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(code)));

        _cache.Set(
            CacheKey(email),
            codeHash,
            TimeSpan.FromMinutes(10));

        return code;
    }

    public bool ValidateAndConsumeCode(string email, string code)
    {
        var key = CacheKey(email);

        if (!_cache.TryGetValue<string>(key, out var storedHash) ||
            string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var suppliedHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(code)));

        var storedBytes = Convert.FromHexString(storedHash);
        var suppliedBytes = Convert.FromHexString(suppliedHash);

        if (!CryptographicOperations.FixedTimeEquals(
                storedBytes, suppliedBytes))
        {
            return false;
        }

        _cache.Remove(key);
        return true;
    }
}
