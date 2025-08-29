using Microsoft.Extensions.Logging;
using Moq;
using ErrorLogPrioritization.Api.Services;

namespace ErrorLogPrioritization.Api.Tests;

public class PerformanceMonitoringServiceTests
{
    private readonly Mock<ILogger<PerformanceMonitoringService>> _mockLogger;
    private readonly PerformanceMonitoringService _performanceMonitoringService;

    public PerformanceMonitoringServiceTests()
    {
        _mockLogger = new Mock<ILogger<PerformanceMonitoringService>>();
        _performanceMonitoringService = new PerformanceMonitoringService(_mockLogger.Object);
    }

    [Fact]
    public void StartOperation_ShouldReturnDisposableTimer()
    {
        // Act
        var timer = _performanceMonitoringService.StartOperation("TestOperation");

        // Assert
        Assert.NotNull(timer);
        Assert.IsAssignableFrom<IDisposable>(timer);
    }

    [Fact]
    public void StartOperation_WhenDisposed_ShouldLogCompletion()
    {
        // Arrange
        var operationName = "TestOperation";

        // Act
        using (var timer = _performanceMonitoringService.StartOperation(operationName))
        {
            // Operation running
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Completed operation: {operationName}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordMetric_ShouldLogMetricInformation()
    {
        // Arrange
        var metricName = "TestMetric";
        var value = 123.45;
        var unit = "ms";

        // Act
        _performanceMonitoringService.RecordMetric(metricName, value, unit);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Performance Metric: {metricName} = {value} {unit}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordJsonOperationMetrics_WithNormalDuration_ShouldLogInformation()
    {
        // Arrange
        var operation = "LoadLogs";
        var duration = TimeSpan.FromSeconds(2);
        var fileSizeBytes = 1024L;

        // Act
        _performanceMonitoringService.RecordJsonOperationMetrics(operation, duration, fileSizeBytes);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"JSON Operation Performance: {operation}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordJsonOperationMetrics_WithSlowDuration_ShouldLogWarning()
    {
        // Arrange
        var operation = "LoadLogs";
        var duration = TimeSpan.FromSeconds(10); // Slow operation
        var fileSizeBytes = 1024L;

        // Act
        _performanceMonitoringService.RecordJsonOperationMetrics(operation, duration, fileSizeBytes);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Slow JSON Operation: {operation}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordCopilotStudioMetrics_WithSuccessfulOperation_ShouldLogInformationAndRecordThroughput()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);
        var success = true;
        var logCount = 100;

        // Act
        _performanceMonitoringService.RecordCopilotStudioMetrics(duration, success, logCount);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Copilot Studio Performance: Success")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify throughput metric is recorded
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Performance Metric: CopilotStudio.LogsPerSecond")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordCopilotStudioMetrics_WithSlowOperation_ShouldLogWarning()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(45); // Slow operation
        var success = true;
        var logCount = 50;

        // Act
        _performanceMonitoringService.RecordCopilotStudioMetrics(duration, success, logCount);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Slow Copilot Studio Operation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordCopilotStudioMetrics_WithFailedOperation_ShouldNotRecordThroughput()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);
        var success = false;
        var logCount = 100;

        // Act
        _performanceMonitoringService.RecordCopilotStudioMetrics(duration, success, logCount);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Copilot Studio Performance: Failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify throughput metric is NOT recorded for failed operations
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Performance Metric: CopilotStudio.LogsPerSecond")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}