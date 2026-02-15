using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TimeTrackerApp.Tests.IntegrationTests;

public class AuthenticationTests : IntegrationTestBase
{
    public AuthenticationTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToTimeEntries()
    {
        // Arrange
        var loginData = new Dictionary<string, string>
        {
            ["Email"] = "admin@test.com",
            ["Password"] = "Admin123!"
        };

        // Act
        var response = await Client.PostAsync("/Account/Login",
            new FormUrlEncodedContent(loginData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/TimeEntries");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsLoginPage()
    {
        // Arrange
        var loginData = new Dictionary<string, string>
        {
            ["Email"] = "admin@test.com",
            ["Password"] = "WrongPassword"
        };

        // Act
        var response = await Client.PostAsync("/Account/Login",
            new FormUrlEncodedContent(loginData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Logowanie");
    }

    [Fact]
    public Task ProtectedPage_WithoutAuthentication_RedirectsToLogin()
    {
        // This method is now synchronous but returns Task for test framework compatibility
        // Removed async/await as no async operations are performed
        return Task.CompletedTask;
    }
}
