using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using ErrorLogPrioritization.Api.Services;

namespace ErrorLogPrioritization.Api.Tests;

public class HealthCheckServiceTests
{
    private readonly Mock<ILogger<ErrorLogPrioritization.Api.Services.HealthCheckService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly ErrorLogPrioritization.Api.Services.HealthCheckService _healthCheckService;

    public HealthCheckServiceTests()
    {
        _mockLogger = new Mock<ILogger<ErrorLogPrioritization.Api.Services.HealthCheckService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _healthCheckService = new ErrorLogPrioritization.Api.Services.HealthCheckService(_mockLogger.Object, _mockConfiguration.Object, _httpClient);
    }

    [Fact]
    public async Task CheckFileSystemHealthAsync_WhenFileSystemAccessible_ShouldReturnHealthy()
    {
        // Act
        var result = await _healthCheckService.CheckFileSystemHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("File system is accessible", result.Description);
    }

    [Fact]
    public async Task CheckCopilotStudioHealthAsync_WhenUrlNotConfigured_ShouldReturnDegraded()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["CopilotStudio:BaseUrl"]).Returns((string?)null);

        // Act
        var result = await _healthCheckService.CheckCopilotStudioHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("Copilot Studio URL not configured", result.Description);
    }

    [Fact]
    public async Task CheckCopilotStudioHealthAsync_WhenServiceRespondsSuccessfully_ShouldReturnHealthy()
    {
        // Arrange
        var baseUrl = "https://test-copilot-studio.com";
        _mockConfiguration.Setup(x => x["CopilotStudio:BaseUrl"]).Returns(baseUrl);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == $"{baseUrl}/health"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await _healthCheckService.CheckCopilotStudioHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Copilot Studio is accessible", result.Description);
    }

    [Fact]
    public async Task CheckCopilotStudioHealthAsync_WhenServiceReturnsError_ShouldReturnDegraded()
    {
        // Arrange
        var baseUrl = "https://test-copilot-studio.com";
        _mockConfiguration.Setup(x => x["CopilotStudio:BaseUrl"]).Returns(baseUrl);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        var result = await _healthCheckService.CheckCopilotStudioHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("Copilot Studio returned status: InternalServerError", result.Description);
    }

    [Fact]
    public async Task CheckCopilotStudioHealthAsync_WhenRequestTimesOut_ShouldReturnDegraded()
    {
        // Arrange
        var baseUrl = "https://test-copilot-studio.com";
        _mockConfiguration.Setup(x => x["CopilotStudio:BaseUrl"]).Returns(baseUrl);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        // Act
        var result = await _healthCheckService.CheckCopilotStudioHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("Copilot Studio health check timed out", result.Description);
    }

    [Fact]
    public async Task CheckCopilotStudioHealthAsync_WhenHttpExceptionThrown_ShouldReturnDegraded()
    {
        // Arrange
        var baseUrl = "https://test-copilot-studio.com";
        _mockConfiguration.Setup(x => x["CopilotStudio:BaseUrl"]).Returns(baseUrl);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _healthCheckService.CheckCopilotStudioHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("Copilot Studio is not accessible", result.Description);
        Assert.NotNull(result.Exception);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}