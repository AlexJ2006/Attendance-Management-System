namespace AttendanceMonitoringSystem.Services;

public static class AuthConstants
{
    // Name of the cookie authentication scheme.
    public const string Scheme = "MyCookieAuth";

    // Sign-in lockout (STRIDE: spoofing). After this many consecutive
    // failed attempts the account is locked for LockoutMinutes.
    public const int MaxFailedLoginAttempts = 5;
    public const int LockoutMinutes = 15;
}
