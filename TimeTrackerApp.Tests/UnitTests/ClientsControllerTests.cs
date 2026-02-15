using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using TimeTrackerApp.Controllers;
using TimeTrackerApp.Data;
using TimeTrackerApp.Models;
using TimeTrackerApp.Models.ViewModels;
using Xunit;

namespace TimeTrackerApp.Tests.UnitTests;

public class ClientsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ClientsController _controller;

    public ClientsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        SeedTestData();

        _controller = new ClientsController(_context);
        SetupControllerContext("Manager");
    }

    private void SeedTestData()
    {
        // Clients
        var client1 = new Client
        {
            Id = 1,
            Name = "ABC Corp",
            Description = "Test client 1",
            Email = "contact@abc.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var client2 = new Client
        {
            Id = 2,
            Name = "XYZ Ltd",
            Description = "Test client 2",
            Email = "info@xyz.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Clients.AddRange(client1, client2);

        // Users
        var managerUser = new User
        {
            Id = 1,
            Email = "manager@test.com",
            FirstName = "Jan",
            LastName = "Manager",
            PasswordHash = "hash",
            Role = UserRole.Manager
        };

        _context.Users.Add(managerUser);

        // Employee (Manager)
        var manager = new Employee
        {
            Id = 1,
            UserId = 1,
            Position = "Manager",
            Department = "IT",
            IsActive = true
        };

        _context.Employees.Add(manager);

        // Projects for client1
        var project1 = new Project
        {
            Id = 1,
            Name = "Project Alpha",
            Description = "Test project",
            ClientId = 1,
            ManagerId = 1,
            Status = ProjectStatus.Active,
            HoursBudget = 100,
            StartDate = DateTime.Today.AddDays(-30)
        };

        var project2 = new Project
        {
            Id = 2,
            Name = "Project Beta",
            Description = "Test project 2",
            ClientId = 1,
            ManagerId = 1,
            Status = ProjectStatus.Completed,
            HoursBudget = 50,
            StartDate = DateTime.Today.AddDays(-60),
            EndDate = DateTime.Today.AddDays(-10)
        };

        _context.Projects.AddRange(project1, project2);

        // Time entries
        var entry1 = new TimeEntry
        {
            Id = 1,
            EmployeeId = 1,
            ProjectId = 1,
            EntryDate = DateTime.Today,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(12),
            Description = "Work",
            CreatedBy = 1
        };

        var entry2 = new TimeEntry
        {
            Id = 2,
            EmployeeId = 1,
            ProjectId = 1,
            EntryDate = DateTime.Today,
            StartTime = TimeSpan.FromHours(13),
            EndTime = TimeSpan.FromHours(17),
            Description = "More work",
            CreatedBy = 1
        };

        _context.TimeEntries.AddRange(entry1, entry2);
        _context.SaveChanges();
    }

    private void SetupControllerContext(string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, "manager@test.com")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        
        var tempData = new TempDataDictionary(httpContext, new FakeTempDataProvider());

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        _controller.TempData = tempData;
    }

    private class FakeTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _data = new();

        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return _data;
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _data.Clear();
            foreach (var kvp in values)
            {
                _data[kvp.Key] = kvp.Value;
            }
        }
    }

    [Fact]
    public async Task Index_ReturnsViewWithClients()
    {
        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        viewResult!.Model.Should().BeAssignableTo<IEnumerable<Client>>();

        var model = viewResult.Model as IEnumerable<Client>;
        model.Should().NotBeNull();
        model!.Should().HaveCount(2);
        model.Should().Contain(c => c.Name == "ABC Corp");
        model.Should().Contain(c => c.Name == "XYZ Ltd");
    }

    [Fact]
    public async Task Details_WithValidId_ReturnsViewWithClient()
    {
        // Act
        var result = await _controller.Details(1);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        viewResult!.Model.Should().BeOfType<Client>();

        var model = viewResult.Model as Client;
        model.Should().NotBeNull();
        model!.Id.Should().Be(1);
        model.Name.Should().Be("ABC Corp");
        model.Projects.Should().HaveCount(2);
    }

    [Fact]
    public async Task Details_WithInvalidId_RedirectsToIndex()
    {
        // Act
        var result = await _controller.Details(999);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("Index");
        _controller.TempData["ErrorMessage"].Should().NotBeNull();
    }

    [Fact]
    public void Create_Get_ReturnsView()
    {
        // Act
        var result = _controller.Create();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Create_Post_WithValidModel_RedirectsToIndex()
    {
        // Arrange
        var newClient = new Client
        {
            Name = "New Client",
            Description = "Test description",
            Email = "new@client.com",
            IsActive = true
        };

        // Act
        var result = await _controller.Create(newClient);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("Index");
        
        var clients = await _context.Clients.ToListAsync();
        clients.Should().HaveCount(3);
        clients.Should().Contain(c => c.Name == "New Client");
    }

    [Fact]
    public async Task Edit_Get_WithValidId_ReturnsViewWithClient()
    {
        // Act
        var result = await _controller.Edit(1);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        viewResult!.Model.Should().BeOfType<Client>();

        var model = viewResult.Model as Client;
        model.Should().NotBeNull();
        model!.Id.Should().Be(1);
        model.Name.Should().Be("ABC Corp");
    }

    [Fact]
    public async Task Edit_Get_WithInvalidId_RedirectsToIndex()
    {
        // Act
        var result = await _controller.Edit(999);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task Edit_Post_WithValidModel_UpdatesClient()
    {
        // Arrange
        var client = await _context.Clients.FindAsync(1);
        client!.Name = "Updated Name";
        client.Description = "Updated description";

        // Act
        var result = await _controller.Edit(1, client);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        
        var updatedClient = await _context.Clients.FindAsync(1);
        updatedClient.Should().NotBeNull();
        updatedClient!.Name.Should().Be("Updated Name");
        updatedClient.Description.Should().Be("Updated description");
        updatedClient.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_Get_WithValidId_ReturnsViewWithClient()
    {
        // Act
        var result = await _controller.Delete(2);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        viewResult!.Model.Should().BeOfType<Client>();

        var model = viewResult.Model as Client;
        model.Should().NotBeNull();
        model!.Id.Should().Be(2);
    }

    [Fact]
    public async Task Delete_Post_ClientWithProjects_RedirectsWithError()
    {
        // Act - Client 1 has 2 projects
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("Index");
        _controller.TempData["ErrorMessage"].Should().NotBeNull();
        
        var client = await _context.Clients.FindAsync(1);
        client.Should().NotBeNull(); // Should still exist
    }

    [Fact]
    public async Task Delete_Post_ClientWithoutProjects_DeletesClient()
    {
        // Arrange - Client 2 has no projects
        // First remove projects from client 2 if any exist
        var client2Projects = _context.Projects.Where(p => p.ClientId == 2).ToList();
        _context.Projects.RemoveRange(client2Projects);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteConfirmed(2);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("Index");
        _controller.TempData["SuccessMessage"].Should().NotBeNull();
        
        var client = await _context.Clients.FindAsync(2);
        client.Should().BeNull();
    }

    [Fact]
    public async Task Report_WithValidId_ReturnsViewWithStatistics()
    {
        // Act
        var result = await _controller.Report(1);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        viewResult!.Model.Should().BeOfType<ClientReportViewModel>();

        var model = viewResult.Model as ClientReportViewModel;
        model.Should().NotBeNull();
        model!.Client.Should().NotBeNull();
        model.Client.Id.Should().Be(1);
        model.ProjectStatistics.Should().HaveCount(2);
        
        // Check summary
        model.Summary.Should().NotBeNull();
        model.Summary.TotalProjects.Should().Be(2);
        model.Summary.ActiveProjects.Should().Be(1);
        model.Summary.CompletedProjects.Should().Be(1);
        model.Summary.TotalHoursAllProjects.Should().Be(7); // 3h + 4h from entries
    }

    [Fact]
    public async Task Report_ChecksBudgetUsageCalculation()
    {
        // Act
        var result = await _controller.Report(1);
        var viewResult = result as ViewResult;
        var model = viewResult!.Model as ClientReportViewModel;

        // Assert - Project 1 has 100h budget, 7h used = 7%
        var project1Stats = model!.ProjectStatistics.First(ps => ps.ProjectId == 1);
        project1Stats.TotalHours.Should().Be(7);
        project1Stats.HoursBudget.Should().Be(100);
        project1Stats.BudgetUsagePercentage.Should().BeApproximately(7, 0.1m);
    }

    [Fact]
    public async Task Report_WithInvalidId_RedirectsToIndex()
    {
        // Act
        var result = await _controller.Report(999);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("Index");
        _controller.TempData["ErrorMessage"].Should().NotBeNull();
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
    }
}
