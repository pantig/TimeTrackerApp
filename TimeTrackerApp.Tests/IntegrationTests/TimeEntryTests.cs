using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TimeTrackerApp.Tests.IntegrationTests;

public class TimeEntryTests : IntegrationTestBase
{
    public TimeEntryTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetTimeEntriesList_AsEmployee_ReturnsOwnEntries()
    {
        // Arrange
        var cookie = await LoginAsAsync("employee", "Employee123!");
        SetAuthCookie(cookie);

        // Act
        var response = await Client.GetAsync("/TimeEntries/Index");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Development work");
    }

    [Fact]
    public async Task AddTimeEntry_WithValidData_CreatesEntry()
    {
        // Arrange
        var cookie = await LoginAsAsync("employee", "Employee123!");
        SetAuthCookie(cookie);

        var entryData = new
        {
            employeeId = 3,
            date = DateTime.Today.ToString("yyyy-MM-dd"),
            startTime = "10:00:00",
            endTime = "18:00:00",
            projectId = 1,
            description = "Test work"
        };

        var content = JsonContent.Create(entryData);

        // Act
        var response = await Client.PostAsync("/Calendar/AddEntry", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTimeEntry_WithValidData_UpdatesEntry()
    {
        // Arrange
        var cookie = await LoginAsAsync("employee", "Employee123!");
        SetAuthCookie(cookie);

        var updateData = new
        {
            id = 1,
            startTime = "09:00:00",
            endTime = "18:00:00",
            projectId = 1,
            description = "Updated work"
        };

        var content = JsonContent.Create(updateData);

        // Act
        var response = await Client.PostAsync("/Calendar/UpdateEntry", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTimeEntry_OwnEntry_DeletesSuccessfully()
    {
        // Arrange
        var cookie = await LoginAsAsync("employee", "Employee123!");
        SetAuthCookie(cookie);

        var deleteData = new { id = 1 };
        var content = JsonContent.Create(deleteData);

        // Act
        var response = await Client.PostAsync("/Calendar/DeleteEntry", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task AddTimeEntry_InvalidTimeRange_ReturnsError()
    {
        // Arrange
        var cookie = await LoginAsAsync("employee", "Employee123!");
        SetAuthCookie(cookie);

        var entryData = new
        {
            employeeId = 3,
            date = DateTime.Today.ToString("yyyy-MM-dd"),
            startTime = "18:00:00", // End before start
            endTime = "10:00:00",
            projectId = 1,
            description = "Invalid entry"
        };

        var content = JsonContent.Create(entryData);

        // Act
        var response = await Client.PostAsync("/Calendar/AddEntry", content);

        // Assert
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task ApproveTimeEntry_AsManager_ApprovesSuccessfully()
    {
        // Arrange
        var cookie = await LoginAsAsync("manager", "Manager123!");
        SetAuthCookie(cookie);

        var approveData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Id", "1")
        });

        // Act
        var response = await Client.PostAsync("/TimeEntries/Approve/1", approveData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task RejectTimeEntry_AsManager_RejectsSuccessfully()
    {
        // Arrange
        var cookie = await LoginAsAsync("manager", "Manager123!");
        SetAuthCookie(cookie);

        var rejectData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Id", "1")
        });

        // Act
        var response = await Client.PostAsync("/TimeEntries/Reject/1", rejectData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }
}
