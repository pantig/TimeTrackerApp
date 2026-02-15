using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeTrackerApp.Data;
using TimeTrackerApp.Models;
using Xunit;

namespace TimeTrackerApp.Tests.IntegrationTests;

public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;

    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString());
                });

                // Seed test data
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                SeedTestData(db);
            });
        });

        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    protected virtual void SeedTestData(ApplicationDbContext db)
    {
        // Test users
        var adminUser = new User
        {
            Id = 1,
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = UserRole.Admin,
            IsActive = true
        };

        var managerUser = new User
        {
            Id = 2,
            Email = "manager@test.com",
            FirstName = "Manager",
            LastName = "User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!"),
            Role = UserRole.Manager,
            IsActive = true
        };

        var employeeUser = new User
        {
            Id = 3,
            Email = "employee@test.com",
            FirstName = "Employee",
            LastName = "User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee123!"),
            Role = UserRole.Employee,
            IsActive = true
        };

        db.Users.AddRange(adminUser, managerUser, employeeUser);

        // Test employees
        var adminEmployee = new Employee
        {
            Id = 1,
            UserId = 1,
            Position = "Administrator",
            Department = "IT",
            HireDate = DateTime.UtcNow.AddYears(-2)
        };

        var managerEmployee = new Employee
        {
            Id = 2,
            UserId = 2,
            Position = "Team Manager",
            Department = "Management",
            HireDate = DateTime.UtcNow.AddYears(-1)
        };

        var employeeEmployee = new Employee
        {
            Id = 3,
            UserId = 3,
            Position = "Software Developer",
            Department = "Development",
            HireDate = DateTime.UtcNow.AddMonths(-6)
        };

        db.Employees.AddRange(adminEmployee, managerEmployee, employeeEmployee);

        // Test projects
        var project1 = new Project
        {
            Id = 1,
            Name = "Project Alpha",
            Description = "Test project Alpha",
            IsActive = true
        };

        var project2 = new Project
        {
            Id = 2,
            Name = "Project Beta",
            Description = "Test project Beta",
            IsActive = true
        };

        db.Projects.AddRange(project1, project2);

        // Test time entries
        var entry1 = new TimeEntry
        {
            Id = 1,
            EmployeeId = 3,
            ProjectId = 1,
            EntryDate = DateTime.Today,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            Description = "Development work",
            CreatedBy = 3
        };

        db.TimeEntries.Add(entry1);
        db.SaveChanges();
    }

    protected async Task<string> LoginAsAsync(string email, string password)
    {
        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("Password", password)
        });

        var response = await Client.PostAsync("/Account/Login", loginData);
        
        // Extract cookies
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var authCookie = cookies.FirstOrDefault(c => c.StartsWith(".AspNetCore.Cookies"));
            return authCookie?.Split(';')[0] ?? string.Empty;
        }

        return string.Empty;
    }

    protected void SetAuthCookie(string cookie)
    {
        if (!string.IsNullOrEmpty(cookie))
        {
            Client.DefaultRequestHeaders.Add("Cookie", cookie);
        }
    }
}
