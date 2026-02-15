using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TimeTrackerApp.Models;
using Xunit;

namespace TimeTrackerApp.Tests.IntegrationTests;

public class ProjectTests : IntegrationTestBase
{
    public ProjectTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetProjectsList_AsAuthenticated_ReturnsProjects()
    {
        // Arrange
        await LoginAsAsync("admin@test.com", "Admin123!");

        // Act
        var response = await Client.GetAsync("/Projects/Index");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Project Alpha");
        content.Should().Contain("Project Beta");
    }

    [Fact]
    public async Task CreateProject_WithValidData_CreatesProject()
    {
        // Arrange
        await LoginAsAsync("admin@test.com", "Admin123!");

        var projectData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Name", "New Project"),
            new KeyValuePair<string, string>("Description", "Test description"),
            new KeyValuePair<string, string>("IsActive", "true")
        });

        // Act
        var response = await Client.PostAsync("/Projects/Create", projectData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Projects/Index");
    }

    [Fact]
    public async Task UpdateProject_WithValidData_UpdatesProject()
    {
        // Arrange
        await LoginAsAsync("admin@test.com", "Admin123!");

        var updateData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Id", "1"),
            new KeyValuePair<string, string>("Name", "Updated Project Alpha"),
            new KeyValuePair<string, string>("Description", "Updated description"),
            new KeyValuePair<string, string>("IsActive", "true")
        });

        // Act
        var response = await Client.PostAsync("/Projects/Edit/1", updateData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task DeleteProject_ExistingProject_DeletesSuccessfully()
    {
        // Arrange
        await LoginAsAsync("admin@test.com", "Admin123!");

        // Act
        var response = await Client.PostAsync("/Projects/Delete/2", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task CreateProject_WithoutAuth_RedirectsToLogin()
    {
        // Act
        var response = await Client.GetAsync("/Projects/Create");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }
}
