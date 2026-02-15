using Microsoft.EntityFrameworkCore;
using TimeTrackerApp.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using TimeTrackerApp.Services;
using TimeTrackerApp.Migrations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<ITimeEntryService, TimeEntryService>();
builder.Services.AddScoped<ExcelExportService>();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ✅ FIXED: Apply migrations (EF Core + Custom SQL)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Only migrate if using a relational database (not InMemory for tests)
    if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
    {
        if (db.Database.IsRelational())
        {
            // Ensure database is created
            db.Database.EnsureCreated();
            
            // Run custom SQL migrations
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    MigrationRunner.RunMigrations(connectionString);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error running migrations: {ex.Message}");
                    throw;
                }
            }
        }
    }
    else
    {
        // For InMemory, just ensure the database is created
        db.Database.EnsureCreated();
    }
}

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
