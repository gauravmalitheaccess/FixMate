using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;
using ErrorLogPrioritization.Api.Middleware;

namespace ErrorLogPrioritization.Api.Tests;

public class GlobalExceptionHandlerMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionHandlerMiddleware>> _mockLogger;
    private readonly GlobalExceptionHandlerMiddleware _middleware;

    public GlobalExceptionHandlerMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        _middleware = new GlobalExceptionHandlerMiddleware(
            next: (context) => throw new InvalidOperationException("Test exception"),
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_ShouldReturnInternalServerError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentExceptionThrown_ShouldReturnBadRequest()
    {
        // Arrange
        var middleware = new GlobalExceptionHandlerMiddleware(
            next: (context) => throw new ArgumentException("Invalid argument"),
            _mockLogger.Object
        );
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenFileNotFoundExceptionThrown_ShouldReturnNotFound()
    {
        // Arrange
        var middleware = new GlobalExceptionHandlerMiddleware(
            next: (context) => throw new FileNotFoundException("File not found"),
            _mockLogger.Object
        );
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenTimeoutExceptionThrown_ShouldReturnRequestTimeout()
    {
        // Arrange
        var middleware = new GlobalExceptionHandlerMiddleware(
            next: (context) => throw new TimeoutException("Operation timed out"),
            _mockLogger.Object
        );
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.RequestTimeout, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_ShouldLogError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An unhandled exception occurred")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_ShouldReturnErrorResponseWithTraceId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "test-trace-id";

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(errorResponse);
        Assert.Equal("test-trace-id", errorResponse.TraceId);
        Assert.NotEmpty(errorResponse.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenNoExceptionThrown_ShouldCallNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new GlobalExceptionHandlerMiddleware(
            next: (context) => { nextCalled = true; return Task.CompletedTask; },
            _mockLogger.Object
        );
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }
}