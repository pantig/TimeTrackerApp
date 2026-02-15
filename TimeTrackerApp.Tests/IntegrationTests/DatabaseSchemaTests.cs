using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeTrackerApp.Data;
using Xunit;

namespace TimeTrackerApp.Tests.IntegrationTests;

public class DatabaseSchemaTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DatabaseSchemaTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Database_Schema_ContainsClientsTable()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act & Assert
        var clientsTableExists = await context.Database.ExecuteSqlRawAsync(
            "SELECT name FROM sqlite_master WHERE type='table' AND name='Clients'") >= 0;

        // Sprawdzenie czy tabela istnieje poprzez próbę zapytania
        var canQueryClients = true;
        try
        {
            var count = await context.Clients.CountAsync();
        }
        catch
        {
            canQueryClients = false;
        }

        canQueryClients.Should().BeTrue("Tabela Clients powinna istnieć w bazie danych");
    }

    [Fact]
    public async Task Database_Schema_ProjectsTableHasClientIdColumn()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act & Assert - Sprawdzenie czy można zapytać o ClientId
        var canQueryClientId = true;
        try
        {
            var projects = await context.Projects
                .Select(p => new { p.Id, p.ClientId })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            canQueryClientId = false;
            Console.WriteLine($"Błąd: {ex.Message}");
        }

        canQueryClientId.Should().BeTrue("Kolumna ClientId powinna istnieć w tabeli Projects");
    }

    [Fact]
    public async Task Database_Schema_ClientsTable_HasRequiredColumns()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Act - Sprawdzenie struktury przez próbę utworzenia obiektu
        var client = new TimeTrackerApp.Models.Client
        {
            Name = "Test Client Schema",
            Description = "Test",
            Email = "test@test.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Clients.Add(client);
        var saved = await context.SaveChangesAsync();

        // Assert
        saved.Should().BeGreaterThan(0, "Powinno być możliwe zapisanie klienta");
        client.Id.Should().BeGreaterThan(0, "ID klienta powinno zostać wygenerowane");

        // Cleanup
        context.Clients.Remove(client);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Database_Schema_ProjectRequiresClient()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Upewnij się że istnieje klient i manager
        var client = new TimeTrackerApp.Models.Client
        {
            Name = "Test Client for Project",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        var user = new TimeTrackerApp.Models.User
        {
            Email = "schema.test@test.com",
            FirstName = "Test",
            LastName = "Manager",
            PasswordHash = "hash",
            Role = TimeTrackerApp.Models.UserRole.Manager,
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var manager = new TimeTrackerApp.Models.Employee
        {
            UserId = user.Id,
            Position = "Test Manager",
            Department = "Test",
            IsActive = true
        };
        context.Employees.Add(manager);
        await context.SaveChangesAsync();

        // Act - Utworzenie projektu z ClientId
        var project = new TimeTrackerApp.Models.Project
        {
            Name = "Test Project Schema",
            Description = "Test",
            Status = TimeTrackerApp.Models.ProjectStatus.Active,
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ManagerId = manager.Id,
            ClientId = client.Id
        };

        context.Projects.Add(project);
        var saved = await context.SaveChangesAsync();

        // Assert
        saved.Should().BeGreaterThan(0, "Projekt z ClientId powinien zostać zapisany");
        project.Id.Should().BeGreaterThan(0);
        project.ClientId.Should().Be(client.Id);

        // Cleanup
        context.Projects.Remove(project);
        context.Employees.Remove(manager);
        context.Users.Remove(user);
        context.Clients.Remove(client);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Database_Schema_EmployeesTable_HasCorrectStructure()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act & Assert - Sprawdzenie czy można zapytać o podstawowe kolumny
        var canQueryEmployees = true;
        try
        {
            var employees = await context.Employees
                .Include(e => e.User)
                .Select(e => new 
                { 
                    e.Id, 
                    e.UserId, 
                    e.Position, 
                    e.Department, 
                    e.IsActive,
                    UserEmail = e.User.Email
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            canQueryEmployees = false;
            Console.WriteLine($"Błąd struktury Employees: {ex.Message}");
        }

        canQueryEmployees.Should().BeTrue("Tabela Employees powinna mieć poprawną strukturę");
    }
}
