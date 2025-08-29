using Microsoft.AspNetCore.Http;
using ErrorLogPrioritization.Api.Middleware;

namespace ErrorLogPrioritization.Api.Tests;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenNoCorrelationIdInRequest_ShouldGenerateNewOne()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(next: (ctx) => 
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.True(context.Response.Headers.ContainsKey("X-Correlation-ID"));
        var correlationId = context.Response.Headers["X-Correlation-ID"].ToString();
        Assert.False(string.IsNullOrEmpty(correlationId));
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task InvokeAsync_WhenCorrelationIdInRequest_ShouldUseExistingOne()
    {
        // Arrange
        var existingCorrelationId = Guid.NewGuid().ToString();
        var context = new DefaultHttpContext();
        context.Request.Headers.Add("X-Correlation-ID", existingCorrelationId);
        
        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(next: (ctx) => 
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.True(context.Response.Headers.ContainsKey("X-Correlation-ID"));
        var responseCorrelationId = context.Response.Headers["X-Correlation-ID"].ToString();
        Assert.Equal(existingCorrelationId, responseCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_WhenEmptyCorrelationIdInRequest_ShouldGenerateNewOne()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers.Add("X-Correlation-ID", "");
        
        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(next: (ctx) => 
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.True(context.Response.Headers.ContainsKey("X-Correlation-ID"));
        var correlationId = context.Response.Headers["X-Correlation-ID"].ToString();
        Assert.False(string.IsNullOrEmpty(correlationId));
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task InvokeAsync_ShouldAlwaysCallNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(next: (ctx) => 
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextMiddlewareThrows_ShouldPropagateException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedException = new InvalidOperationException("Test exception");
        var middleware = new CorrelationIdMiddleware(next: (ctx) => throw expectedException);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
        Assert.Equal(expectedException, actualException);
        
        // Correlation ID should still be set even when exception occurs
        Assert.True(context.Response.Headers.ContainsKey("X-Correlation-ID"));
    }
}