using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=attendance.db";

// Register Entity Framework Core with the existing SQLite database (ADR03).
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Register MVC controllers, views and Razor Pages.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Application-layer services.
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<ModuleService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<CourseLeaderService>();
builder.Services.AddScoped<ModuleLeaderService>();
builder.Services.AddScoped<AccessScopeService>();
builder.Services.AddScoped<ReportService>();

// Memory cache backs password reset codes and attendance-code attempt limits.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<PasswordResetService>();
builder.Services.AddSingleton<CodeAttemptLimiter>();

// Automatically update teaching session statuses (ADR07).
builder.Services.AddHostedService<SessionStatusBackgroundService>();

// Health check used to measure availability (NFR01).
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

// Cookie authentication. Passwords are hashed with the ASP.NET Core Identity
// PasswordHasher (ADR04) and roles are added to the cookie as claims.
builder.Services.AddAuthentication(AuthConstants.Scheme)
    .AddCookie(AuthConstants.Scheme, options =>
    {
        options.LoginPath = "/Index";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.Cookie.Name = "AMS.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Every page requires a signed-in user unless it is explicitly marked
// [AllowAnonymous] (sign-in, password reset, privacy and error pages).
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Keep the existing optional password setup process.
if (Environment.GetEnvironmentVariable("RUN_PASSWORD_SETUP") == "1")
{
    var email = Environment.GetEnvironmentVariable("INITIAL_USER_EMAIL");
    var password = Environment.GetEnvironmentVariable("INITIAL_PASSWORD");

    if (string.IsNullOrWhiteSpace(email) ||
        string.IsNullOrWhiteSpace(password))
    {
        throw new InvalidOperationException(
            "Set INITIAL_USER_EMAIL and INITIAL_PASSWORD first.");
    }

    await PasswordSetup.ResetPasswordAsync(
        connectionString, email, password);

    return;
}

var app = builder.Build();

// Bring the database schema and roles up to date before serving requests.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DatabaseInitialiser.InitialiseAsync(context, logger);
}

// Show a friendly error page rather than a stack trace when something fails,
// e.g. the database is unavailable (NFR01). Set ShowDetailedErrors to true
// in appsettings.Development.json to see the developer exception page.
if (app.Environment.IsDevelopment() &&
    app.Configuration.GetValue<bool>("ShowDetailedErrors"))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }
}

// Friendly pages for 403, 404 and similar responses.
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");

// Basic security headers.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseStaticFiles();
app.UseRouting();

// Authentication must run before authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

// Allows the test project and tools to reference the application.
public partial class Program { }
