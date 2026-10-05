var builder = WebApplication.CreateBuilder(args);

// Add services for MVC controllers and Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add cookie authentication
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Index";
        options.AccessDeniedPath = "/Index";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Enable MVC controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Enable Razor Pages
app.MapRazorPages();

app.Run();