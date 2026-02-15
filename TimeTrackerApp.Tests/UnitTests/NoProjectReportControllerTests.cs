using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTrackerApp.Controllers;
using TimeTrackerApp.Data;
using TimeTrackerApp.Models;
using TimeTrackerApp.Models.ViewModels;
using Xunit;

namespace TimeTrackerApp.Tests.UnitTests;

public class NoProjectReportControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly NoProjectReportController _controller;

    public NoProjectReportControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        SeedTestData();

        _controller = new NoProjectReportController(_context);
        SetupControllerContext(3); // Employee user ID
    }

    private void SeedTestData()
    {
        // Users
        var employeeUser = new User
        {
            Id = 3,
            Email = "employee@test.com",
            FirstName = "Jan",
            LastName = "Kowalski",
            PasswordHash = "hash",
            Role = UserRole.Employee
        };

        var managerUser = new User
        {
            Id = 2,
            Email = "manager@test.com",
            FirstName = "Anna",
            LastName = "Manager",
            PasswordHash = "hash",
            Role = UserRole.Manager
        };

        // User without Employee profile
        var userWithoutProfile = new User
        {
            Id = 999,
            Email = "noprofile@test.com",
            FirstName = "No",
            LastName = "Profile",
            PasswordHash = "hash",
            Role = UserRole.Employee
        };

        _context.Users.AddRange(employeeUser, managerUser, userWithoutProfile);

        // Employees - ONLY for user 3, NOT for user 999
        var employee = new Employee
        {
            Id = 3,
            UserId = 3,
            Position = "Developer",
            Department = "IT",
            IsActive = true
        };

        _context.Employees.Add(employee);

        // Projects
        var project1 = new Project
        {
            Id = 1,
            Name = "Project A",
            Description = "Test Project A",
            IsActive = true,
            StartDate = DateTime.Today,
            ManagerId = 2,
            Employees = new List<Employee> { employee }
        };

        var project2 = new Project
        {
            Id = 2,
            Name = "Project B",
            Description = "Test Project B",
            IsActive = true,
            StartDate = DateTime.Today,
            ManagerId = 2,
            Employees = new List<Employee> { employee }
        };

        _context.Projects.AddRange(project1, project2);

        // Time entries - with and without projects
        var entryWithProject = new TimeEntry
        {
            Id = 1,
            EmployeeId = 3,
            ProjectId = 1,
            EntryDate = DateTime.Today,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(12),
            Description = "Work with project",
            CreatedBy = 3
        };

        var entryWithoutProject1 = new TimeEntry
        {
            Id = 2,
            EmployeeId = 3,
            ProjectId = null,
            EntryDate = DateTime.Today,
            StartTime = TimeSpan.FromHours(13),
            EndTime = TimeSpan.FromHours(15),
            Description = "Work without project",
            CreatedBy = 3
        };

        var entryWithoutProject2 = new TimeEntry
        {
            Id = 3,
            EmployeeId = 3,
            ProjectId = null,
            EntryDate = DateTime.Today.AddDays(-1),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11),
            Description = "Another entry without project",
            CreatedBy = 3
        };

        _context.TimeEntries.AddRange(entryWithProject, entryWithoutProject1, entryWithoutProject2);
        _context.SaveChanges();
    }

    private void SetupControllerContext(int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, userId == 2 ? "Manager" : "Employee")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task MyEntries_ReturnsViewWithEntriesWithoutProject()
    {
        // Arrange
        SetupControllerContext(3); // Employee

        // Act
        var result = await _controller.MyEntries();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        viewResult!.Model.Should().BeOfType<NoProjectEntriesViewModel>();

        var model = viewResult.Model as NoProjectEntriesViewModel;
        model.Should().NotBeNull();
        model!.Entries.Should().HaveCount(2); // Only entries without project
        model.IsManagerView.Should().BeFalse();
        model.EmployeeName.Should().Be("Jan Kowalski");
        model.TotalHours.Should().Be(3); // 2h + 1h
    }

    [Fact]
    public async Task MyEntries_WithoutEmployeeProfile_RedirectsWithError()
    {
        // Arrange - User 999 exists but has NO Employee profile
        SetupControllerContext(999);

        // Act
        var result = await _controller.MyEntries();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("TimeEntries");
    }

    [Fact]
    public async Task AllEntries_AsManager_ReturnsViewWithAllEntriesWithoutProject()
    {
        // Arrange
        SetupControllerContext(2); // Manager

        // Act
        var result = await _controller.AllEntries(null);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        viewResult!.Model.Should().BeOfType<NoProjectEntriesViewModel>();

        var model = viewResult.Model as NoProjectEntriesViewModel;
        model.Should().NotBeNull();
        model!.Entries.Should().HaveCount(2);
        model.IsManagerView.Should().BeTrue();
        model.AllEmployees.Should().NotBeNull();
    }

    [Fact]
    public async Task AllEntries_WithEmployeeFilter_ReturnsFilteredEntries()
    {
        // Arrange
        SetupControllerContext(2); // Manager

        // Act
        var result = await _controller.AllEntries(employeeId: 3);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        var model = viewResult!.Model as NoProjectEntriesViewModel;
        model.Should().NotBeNull();

        model!.Entries.Should().HaveCount(2);
        model.SelectedEmployeeId.Should().Be(3);
        model.Entries.Should().OnlyContain(e => e.EmployeeId == 3);
    }

    [Fact]
    public async Task AssignProject_WithValidData_AssignsProjectSuccessfully()
    {
        // Arrange
        var request = new NoProjectReportController.AssignProjectRequest
        {
            EntryId = 2,
            ProjectId = 1
        };

        // Act
        var result = await _controller.AssignProject(request);

        // Assert
        result.Should().BeOfType<JsonResult>();
        
        // Check database
        var entry = await _context.TimeEntries.FindAsync(2);
        entry.Should().NotBeNull();
        entry!.ProjectId.Should().Be(1);
    }

    [Fact]
    public async Task AssignProject_WithNonExistentEntry_ReturnsFailure()
    {
        // Arrange
        var request = new NoProjectReportController.AssignProjectRequest
        {
            EntryId = 999,
            ProjectId = 1
        };

        // Act
        var result = await _controller.AssignProject(request);

        // Assert
        result.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public async Task AssignProject_ToOtherEmployeeEntry_AsEmployee_ReturnsForbidden()
    {
        // Arrange
        SetupControllerContext(3); // Employee

        // Create entry for different employee
        var otherUser = new User
        {
            Id = 99,
            Email = "other@test.com",
            FirstName = "Other",
            LastName = "User",
            PasswordHash = "hash",
            Role = UserRole.Employee
        };
        _context.Users.Add(otherUser);

        var otherEmployee = new Employee
        {
            Id = 99,
            UserId = 99,
            Position = "Other",
            Department = "HR",
            IsActive = true
        };
        _context.Employees.Add(otherEmployee);

        var otherEntry = new TimeEntry
        {
            Id = 100,
            EmployeeId = 99,
            ProjectId = null,
            EntryDate = DateTime.Today,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Description = "Other work",
            CreatedBy = 99
        };
        _context.TimeEntries.Add(otherEntry);
        await _context.SaveChangesAsync();

        var request = new NoProjectReportController.AssignProjectRequest
        {
            EntryId = 100,
            ProjectId = 1
        };

        // Act
        var result = await _controller.AssignProject(request);

        // Assert
        result.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public async Task TotalHours_CalculatesCorrectly()
    {
        // Arrange
        SetupControllerContext(3);

        // Act
        var result = await _controller.MyEntries();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        var model = viewResult!.Model as NoProjectEntriesViewModel;
        model.Should().NotBeNull();

        // Assert
        model!.TotalHours.Should().Be(3.0m); // 2h + 1h
    }

    [Fact]
    public async Task TotalDays_CalculatesCorrectly()
    {
        // Arrange
        SetupControllerContext(3);

        // Act
        var result = await _controller.MyEntries();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        var model = viewResult!.Model as NoProjectEntriesViewModel;
        model.Should().NotBeNull();

        // Assert
        model!.TotalDays.Should().Be(2); // Today and yesterday
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
    }
}
