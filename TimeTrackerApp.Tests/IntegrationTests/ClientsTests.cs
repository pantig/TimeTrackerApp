using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TimeTrackerApp.Tests.IntegrationTests;

public class ClientsTests : IntegrationTestBase
{
    public ClientsTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task Index_WithoutAuth_RedirectsToLogin()
    {
        // Act
        var response = await Client.GetAsync("/Clients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }

    [Fact]
    public async Task Index_AsEmployee_ReturnsForbidden()
    {
        // Arrange
        await LoginAsAsync("employee@test.com", "Employee123!");

        // Act
        var response = await Client.GetAsync("/Clients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Index_AsManager_ReturnsSuccess()
    {
        // Arrange
        await LoginAsAsync("manager@test.com", "Manager123!");

        // Act
        var response = await Client.GetAsync("/Clients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Klienci");
    }

    [Fact]
    public async Task Index_AsAdmin_ReturnsSuccess()
    {
        // Arrange
        await LoginAsAsync("admin@test.com", "Admin123!");

        // Act
        var response = await Client.GetAsync("/Clients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_Get_AsManager_ReturnsSuccess()
    {
        // Arrange
        await LoginAsAsync("manager@test.com", "Manager123!");

        // Act
        var response = await Client.GetAsync("/Clients/Create");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Dodaj klienta");
    }

    [Fact]
    public async Task Create_Post_AsManager_CreatesClient()
    {
        // Arrange
        await LoginAsAsync("manager@test.com", "Manager123!");
        
        // First, get the create page to obtain anti-forgery token
        var getResponse = await Client.GetAsync("/Clients/Create");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract anti-forgery token (simplified - in real tests use HtmlAgilityPack)
        var token = ExtractAntiForgeryToken(getContent);

        var formData = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "Test Client",
            ["Description"] = "Integration test client",
            ["Email"] = "test@client.com",
            ["Phone"] = "123456789",
            ["IsActive"] = "true"
        };

        var content = new FormUrlEncodedContent(formData);

        // Act
        var response = await Client.PostAsync("/Clients/Create", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Clients");
    }

    [Fact]
    public async Task Details_AsManager_ReturnsSuccess()
    {
        // Arrange
        await LoginAsAsync("manager@test.com", "Manager123!");

        // Act - assuming client with Id=1 exists from seeded data
        var response = await Client.GetAsync("/Clients/Details/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Szczegóły klienta");
    }

    [Fact]
    public async Task Details_WithInvalidId_RedirectsToIndex()
    {
        // Arrange
        await LoginAsAsync("manager@test.com", "Manager123!");

        // Act
        var response = await Client.GetAsync("/Clients/Details/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Clients");
    }

    [Fact]
    public async Task Edit_Get_AsManager_ReturnsSuccess()
    {
        // Arrange
        await LoginAsAsync("manager@test.com", "Manager123!");

        // Act
        var response = await Client.GetAsync("/Clients/Edit/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Edytuj klienta");
    }

    [Fact]
    public async Task Delete_Get_AsManager_ReturnsSuccess()
    {
        // Arrange
        await LoginAsAsync("manager@test.com", "Manager123!");

        // Act
        var response = await Client.GetAsync("/Clients/Delete/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Czy na pewno");
    }

    [Fact]
    public async Task Report_AsManager_ReturnsSuccess()
    {
        // Arrange
        await LoginAsAsync("manager@test.com", "Manager123!");

        // Act
        var response = await Client.GetAsync("/Clients/Report/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Raport klienta");
        content.Should().Contain("Statystyki projektów");
    }

    [Fact]
    public async Task Report_WithInvalidId_RedirectsToIndex()
    {
        // Arrange
        await LoginAsAsync("manager@test.com", "Manager123!");

        // Act
        var response = await Client.GetAsync("/Clients/Report/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Clients");
    }

    [Fact]
    public async Task Report_AsEmployee_ReturnsForbidden()
    {
        // Arrange
        await LoginAsAsync("employee@test.com", "Employee123!");

        // Act
        var response = await Client.GetAsync("/Clients/Report/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private string ExtractAntiForgeryToken(string htmlContent)
    {
        // Simple extraction - in production use HtmlAgilityPack
        var tokenStart = htmlContent.IndexOf("__RequestVerificationToken");
        if (tokenStart == -1) return string.Empty;
        
        var valueStart = htmlContent.IndexOf("value=\"", tokenStart);
        if (valueStart == -1) return string.Empty;
        
        valueStart += 7; // length of 'value="'
        var valueEnd = htmlContent.IndexOf("\"", valueStart);
        
        if (valueEnd == -1) return string.Empty;
        
        return htmlContent.Substring(valueStart, valueEnd - valueStart);
    }
}
