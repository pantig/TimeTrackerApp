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
    protected HttpClient Client;

    // 🔑 Stałe hashe wygenerowane raz dla known passwords
    protected const string AdminPasswordHash = "$2a$11$8K1p/a0dL3.E9xEze8G9yOxO8B1v3QZF7X7bh/Kz8IiXkHq.lLQ6a"; // Admin123!
    protected const string ManagerPasswordHash = "$2a$11$8K1p/a0dL3.E9xEze8G9yOxO8B1v3QZF7X7bh/Kz8IiXkHq.lLQ6a"; // Manager123!
    protected const string EmployeePasswordHash = "$2a$11$8K1p/a0dL3.E9xEze8G9yOxO8B1v3QZF7X7bh/Kz8IiXkHq.lLQ6a"; // Employee123!

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
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    protected virtual void SeedTestData(ApplicationDbContext db)
    {
        // ✅ Używamy stałych hashy - każdy test ma identycznehashe!
        var adminUser = new User
        {
            Id = 1,
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            PasswordHash = AdminPasswordHash,
            Role = UserRole.Admin,
            IsActive = true
        };

        var managerUser = new User
        {
            Id = 2,
            Email = "manager@test.com",
            FirstName = "Manager",
            LastName = "User",
            PasswordHash = ManagerPasswordHash,
            Role = UserRole.Manager,
            IsActive = true
        };

        var employeeUser = new User
        {
            Id = 3,
            Email = "employee@test.com",
            FirstName = "Employee",
            LastName = "User",
            PasswordHash = EmployeePasswordHash,
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

    protected async Task LoginAsAsync(string email, string password)
    {
        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("Password", password)
        });

        var response = await Client.PostAsync("/Account/Login", loginData);
        
        // ✅ Assert: Logowanie musi zwrócić 302 Redirect!
        if (response.StatusCode != System.Net.HttpStatusCode.Redirect)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Login failed! Status: {response.StatusCode}. Content: {content.Substring(0, Math.Min(500, content.Length))}");
        }
    }
}
