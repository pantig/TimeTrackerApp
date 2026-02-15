using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TimeTrackerApp.Tests.IntegrationTests;

public class ExportBlockTests : IntegrationTestBase
{
    public ExportBlockTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task ExportMonthlyExcel_WithEntriesWithoutProject_IsBlocked()
    {
        // Arrange
        await LoginAsAsync("employee@test.com", "Employee123!");

        // Create entry without project
        var entryData = new
        {
            employeeId = 3,
            date = DateTime.Today.ToString("yyyy-MM-dd"),
            startTime = "10:00:00",
            endTime = "12:00:00",
            projectId = (int?)null,
            description = "Entry without project"
        };

        await Client.PostAsJsonAsync("/Calendar/AddEntry", entryData);

        // Act - Try to export
        var year = DateTime.Today.Year;
        var month = DateTime.Today.Month;
        var response = await Client.GetAsync($"/Reports/ExportMonthlyExcel?employeeId=3&year={year}&month={month}");

        // Assert - Should redirect back to Monthly report with error
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Reports/Monthly");
    }

    [Fact]
    public async Task ExportMonthlyExcel_WithAllProjectsAssigned_AllowsExport()
    {
        // Arrange
        await LoginAsAsync("employee@test.com", "Employee123!");

        // Create entry WITH project
        var entryData = new
        {
            employeeId = 3,
            date = DateTime.Today.ToString("yyyy-MM-dd"),
            startTime = "10:00:00",
            endTime = "12:00:00",
            projectId = 1,
            description = "Entry with project"
        };

        await Client.PostAsJsonAsync("/Calendar/AddEntry", entryData);

        // Act - Try to export
        var year = DateTime.Today.Year;
        var month = DateTime.Today.Month;
        var response = await Client.GetAsync($"/Reports/ExportMonthlyExcel?employeeId=3&year={year}&month={month}");

        // Assert - Should allow download
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task ExportMonthlyExcel_AfterAssigningProject_AllowsExport()
    {
        // Arrange
        await LoginAsAsync("employee@test.com", "Employee123!");

        // 1. Create entry without project
        var entryData = new
        {
            employeeId = 3,
            date = DateTime.Today.ToString("yyyy-MM-dd"),
            startTime = "10:00:00",
            endTime = "12:00:00",
            projectId = (int?)null,
            description = "Entry initially without project"
        };

        var createResponse = await Client.PostAsJsonAsync("/Calendar/AddEntry", entryData);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Assign project
        var assignData = new
        {
            entryId = 1,
            projectId = 1
        };

        var assignResponse = await Client.PostAsJsonAsync("/NoProjectReport/AssignProject", assignData);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Now export should work
        var year = DateTime.Today.Year;
        var month = DateTime.Today.Month;
        var exportResponse = await Client.GetAsync($"/Reports/ExportMonthlyExcel?employeeId=3&year={year}&month={month}");

        // Assert
        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
