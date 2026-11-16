using AttendanceMonitoringSystem.Data;
using AttendanceMonitoringSystem.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register Entity Framework Core with the existing SQLite database.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=attendance.db"));

// Register MVC controllers and views.
builder.Services.AddControllersWithViews();

// Register the services used by the application.
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<ModuleService>();
builder.Services.AddScoped<AttendanceService>();

// New service for Course Leader dashboards and read-only monitoring.
builder.Services.AddScoped<CourseLeaderService>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<PasswordResetService>();

// Automatically update teaching session statuses.
builder.Services.AddHostedService<SessionStatusBackgroundService>();

// Register Razor Pages.
builder.Services.AddRazorPages();

// Configure cookie authentication.
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Index";
        options.AccessDeniedPath = "/Index";
    });

// Register authorization.
builder.Services.AddAuthorization();

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

    var connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=attendance.db";

    await PasswordSetup.ResetPasswordAsync(
        connectionString, email, password);

    return;
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication must run before authorization.
app.UseAuthentication();
app.UseAuthorization();

// Configure MVC routing.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map Razor Pages.
app.MapRazorPages();

app.Run();