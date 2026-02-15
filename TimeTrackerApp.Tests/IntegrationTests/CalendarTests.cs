using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TimeTrackerApp.Tests.IntegrationTests;

public class CalendarTests : IntegrationTestBase
{
    public CalendarTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetCalendarView_AsEmployee_ReturnsCurrentWeek()
    {
        // Arrange
        var cookie = await LoginAsAsync("employee", "Employee123!");
        SetAuthCookie(cookie);

        // Act
        var response = await Client.GetAsync("/Calendar/Index");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("timegrid-container");
        content.Should().Contain("Development work");
    }

    [Fact]
    public async Task GetCalendarView_WithSpecificDate_ReturnsCorrectWeek()
    {
        // Arrange
        var cookie = await LoginAsAsync("employee", "Employee123!");
        SetAuthCookie(cookie);
        var date = "2026-02-10";

        // Act
        var response = await Client.GetAsync($"/Calendar/Index?date={date}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("timegrid-container");
    }

    [Fact]
    public async Task SetDayMarker_WithValidData_SetsMarker()
    {
        // Arrange
        var cookie = await LoginAsAsync("employee", "Employee123!");
        SetAuthCookie(cookie);

        var markerData = new
        {
            employeeId = 3,
            date = DateTime.Today.ToString("yyyy-MM-dd"),
            type = 1, // BusinessTrip
            note = (string?)null
        };

        var content = JsonContent.Create(markerData);

        // Act
        var response = await Client.PostAsync("/Calendar/SetDayMarker", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task RemoveDayMarker_ExistingMarker_RemovesSuccessfully()
    {
        // Arrange
        var cookie = await LoginAsAsync("employee", "Employee123!");
        SetAuthCookie(cookie);

        // First set a marker
        var markerData = new
        {
            employeeId = 3,
            date = DateTime.Today.ToString("yyyy-MM-dd"),
            type = 2, // DayOff
            note = (string?)null
        };
        await Client.PostAsync("/Calendar/SetDayMarker", JsonContent.Create(markerData));

        // Now remove it
        var removeData = new
        {
            employeeId = 3,
            date = DateTime.Today.ToString("yyyy-MM-dd")
        };

        var content = JsonContent.Create(removeData);

        // Act
        var response = await Client.PostAsync("/Calendar/RemoveDayMarker", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task NavigateCalendar_PreviousWeek_ShowsPreviousWeek()
    {
        // Arrange
        var cookie = await LoginAsAsync("employee", "Employee123!");
        SetAuthCookie(cookie);
        var today = DateTime.Today;
        var prevWeek = today.AddDays(-7).ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/Calendar/Index?date={prevWeek}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NavigateCalendar_NextWeek_ShowsNextWeek()
    {
        // Arrange
        var cookie = await LoginAsAsync("employee", "Employee123!");
        SetAuthCookie(cookie);
        var today = DateTime.Today;
        var nextWeek = today.AddDays(7).ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/Calendar/Index?date={nextWeek}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
