using Microsoft.Extensions.Logging;
using Moq;
using ErrorLogPrioritization.Api.Services;

namespace ErrorLogPrioritization.Api.Tests;

public class RetryServiceTests
{
    private readonly Mock<ILogger<RetryService>> _mockLogger;
    private readonly RetryService _retryService;

    public RetryServiceTests()
    {
        _mockLogger = new Mock<ILogger<RetryService>>();
        _retryService = new RetryService(_mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WhenOperationSucceeds_ShouldReturnResult()
    {
        // Arrange
        var expectedResult = "success";
        var operation = () => Task.FromResult(expectedResult);

        // Act
        var result = await _retryService.ExecuteWithRetryAsync(operation);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WhenOperationFailsWithNonTransientError_ShouldNotRetry()
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            throw new ArgumentException("Non-transient error");
            return Task.FromResult("never reached");
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _retryService.ExecuteWithRetryAsync(operation));
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WhenOperationFailsWithTransientError_ShouldRetry()
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            if (callCount < 3)
                throw new HttpRequestException("Transient error");
            return Task.FromResult("success");
        };

        // Act
        var result = await _retryService.ExecuteWithRetryAsync(operation);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WhenOperationAlwaysFails_ShouldThrowAfterMaxRetries()
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            throw new HttpRequestException("Always fails");
            return Task.FromResult("never reached");
        };

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _retryService.ExecuteWithRetryAsync(operation, maxRetries: 2));
        Assert.Equal(3, callCount); // Initial attempt + 2 retries
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WhenOperationFails_ShouldLogWarnings()
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            if (callCount < 2)
                throw new TimeoutException("Timeout error");
            return Task.FromResult("success");
        };

        // Act
        await _retryService.ExecuteWithRetryAsync(operation, maxRetries: 2);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation failed on attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_VoidOperation_WhenOperationSucceeds_ShouldComplete()
    {
        // Arrange
        var operationCalled = false;
        var operation = () =>
        {
            operationCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await _retryService.ExecuteWithRetryAsync(operation);

        // Assert
        Assert.True(operationCalled);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_VoidOperation_WhenOperationFailsWithTransientError_ShouldRetry()
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            if (callCount < 2)
                throw new HttpRequestException("Transient error");
            return Task.CompletedTask;
        };

        // Act
        await _retryService.ExecuteWithRetryAsync(operation);

        // Assert
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithCustomDelay_ShouldUseExponentialBackoff()
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            if (callCount < 3)
                throw new HttpRequestException("Transient error");
            return Task.FromResult("success");
        };

        var startTime = DateTime.UtcNow;

        // Act
        await _retryService.ExecuteWithRetryAsync(operation, maxRetries: 2, baseDelay: TimeSpan.FromMilliseconds(100));

        // Assert
        var elapsed = DateTime.UtcNow - startTime;
        Assert.True(elapsed.TotalMilliseconds >= 300); // 100ms + 200ms delays
        Assert.Equal(3, callCount);
    }
}