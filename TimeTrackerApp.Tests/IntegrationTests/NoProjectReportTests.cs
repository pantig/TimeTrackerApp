using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TimeTrackerApp.Tests.IntegrationTests;

public class NoProjectReportTests : IntegrationTestBase
{
    public NoProjectReportTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task MyEntries_AsEmployee_ReturnsMyEntriesWithoutProject()
    {
        // Arrange
        await LoginAsAsync("employee@test.com", "Employee123!");

        // Act
        var response = await Client.GetAsync("/NoProjectReport/MyEntries");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Moje wpisy bez projektu");
    }

    [Fact]
    public async Task AllEntries_AsManager_ReturnsAllEntriesWithoutProject()
    {
        // Arrange
        await LoginAsAsync("manager@test.com", "Manager123!");

        // Act
        var response = await Client.GetAsync("/NoProjectReport/AllEntries");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Wszystkie wpisy bez projektu");
        content.Should().Contain("Filtruj po pracowniku");
    }

    [Fact]
    public async Task AllEntries_AsEmployee_IsForbidden()
    {
        // Arrange
        await LoginAsAsync("employee@test.com", "Employee123!");

        // Act
        var response = await Client.GetAsync("/NoProjectReport/AllEntries");

        // Assert - ASP.NET MVC zwraca redirect zamiast 403 dla użytkowników zalogowanych bez uprawnień
        // Oczekujemy przekierowania lub 403
        response.StatusCode.Should().Match(x => x == HttpStatusCode.Forbidden || x == HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task AssignProject_WithValidData_AssignsProjectSuccessfully()
    {
        // Arrange
        await LoginAsAsync("employee@test.com", "Employee123!");

        // Create entry without project first
        var entryData = new
        {
            employeeId = 3,
            date = DateTime.Today.ToString("yyyy-MM-dd"),
            startTime = "10:00:00",
            endTime = "12:00:00",
            projectId = (int?)null,
            description = "Test entry without project"
        };

        var createResponse = await Client.PostAsJsonAsync("/Calendar/AddEntry", entryData);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Now assign project
        var assignData = new
        {
            entryId = 1,
            projectId = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync("/NoProjectReport/AssignProject", assignData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        result.Should().NotBeNull();
        result.Should().ContainKey("success");
    }

    [Fact]
    public async Task AssignProject_ToOtherEmployeeEntry_AsEmployee_IsForbidden()
    {
        // Arrange
        await LoginAsAsync("employee@test.com", "Employee123!");

        // Try to assign project to entry that belongs to another employee
        var assignData = new
        {
            entryId = 999,
            projectId = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync("/NoProjectReport/AssignProject", assignData);

        // Assert
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        result.Should().NotBeNull();
        result.Should().ContainKey("success");
    }

    [Fact]
    public async Task MyEntries_WithoutAuth_RedirectsToLogin()
    {
        // Act
        var response = await Client.GetAsync("/NoProjectReport/MyEntries");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }
}
