using Serilog.Context;

namespace ErrorLogPrioritization.Api.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        
        // Add correlation ID to response headers
        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);
        
        // Add correlation ID to Serilog log context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId))
        {
            var headerValue = correlationId.FirstOrDefault();
            return !string.IsNullOrWhiteSpace(headerValue) ? headerValue : GenerateCorrelationId();
        }

        // Generate new correlation ID if not provided
        return GenerateCorrelationId();
    }

    private static string GenerateCorrelationId()
    {
        return Guid.NewGuid().ToString("D");
    }
}