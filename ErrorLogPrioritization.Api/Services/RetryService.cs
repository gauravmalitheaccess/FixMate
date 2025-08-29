using System.Net.Sockets;

namespace ErrorLogPrioritization.Api.Services;

public interface IRetryService
{
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3, TimeSpan? baseDelay = null);
    Task ExecuteWithRetryAsync(Func<Task> operation, int maxRetries = 3, TimeSpan? baseDelay = null);
}

public class RetryService : IRetryService
{
    private readonly ILogger<RetryService> _logger;

    public RetryService(ILogger<RetryService> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3, TimeSpan? baseDelay = null)
    {
        var delay = baseDelay ?? TimeSpan.FromSeconds(1);
        Exception? lastException = null;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (IsTransientException(ex) && attempt < maxRetries)
            {
                lastException = ex;
                var currentDelay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * Math.Pow(2, attempt));
                
                _logger.LogWarning(ex, "Operation failed on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms", 
                    attempt + 1, maxRetries + 1, currentDelay.TotalMilliseconds);
                
                await Task.Delay(currentDelay);
            }
        }

        _logger.LogError(lastException, "Operation failed after {MaxRetries} attempts", maxRetries + 1);
        throw lastException ?? new InvalidOperationException("Operation failed after maximum retry attempts");
    }

    public async Task ExecuteWithRetryAsync(Func<Task> operation, int maxRetries = 3, TimeSpan? baseDelay = null)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true;
        }, maxRetries, baseDelay);
    }

    private static bool IsTransientException(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => true,
            TimeoutException => true,
            TaskCanceledException => true,
            SocketException => true,
            IOException => true,
            _ => false
        };
    }
}