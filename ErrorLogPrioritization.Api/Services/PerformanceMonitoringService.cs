using System.Diagnostics;

namespace ErrorLogPrioritization.Api.Services;

public interface IPerformanceMonitoringService
{
    IDisposable StartOperation(string operationName);
    void RecordMetric(string metricName, double value, string? unit = null);
    void RecordJsonOperationMetrics(string operation, TimeSpan duration, long fileSizeBytes);
    void RecordCopilotStudioMetrics(TimeSpan duration, bool success, int logCount);
}

public class PerformanceMonitoringService : IPerformanceMonitoringService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;

    public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
    {
        _logger = logger;
    }

    public IDisposable StartOperation(string operationName)
    {
        return new OperationTimer(operationName, _logger);
    }

    public void RecordMetric(string metricName, double value, string? unit = null)
    {
        _logger.LogInformation("Performance Metric: {MetricName} = {Value} {Unit}", 
            metricName, value, unit ?? "");
    }

    public void RecordJsonOperationMetrics(string operation, TimeSpan duration, long fileSizeBytes)
    {
        _logger.LogInformation("JSON Operation Performance: {Operation} completed in {Duration}ms, File Size: {FileSizeKB}KB",
            operation, duration.TotalMilliseconds, fileSizeBytes / 1024.0);

        // Log warning if operation is slow
        if (duration.TotalSeconds > 5)
        {
            _logger.LogWarning("Slow JSON Operation: {Operation} took {Duration}ms for {FileSizeKB}KB file",
                operation, duration.TotalMilliseconds, fileSizeBytes / 1024.0);
        }
    }

    public void RecordCopilotStudioMetrics(TimeSpan duration, bool success, int logCount)
    {
        _logger.LogInformation("Copilot Studio Performance: {Status} in {Duration}ms for {LogCount} logs",
            success ? "Success" : "Failed", duration.TotalMilliseconds, logCount);

        // Log warning if operation is slow
        if (duration.TotalSeconds > 30)
        {
            _logger.LogWarning("Slow Copilot Studio Operation: took {Duration}ms for {LogCount} logs",
                duration.TotalMilliseconds, logCount);
        }

        // Record throughput metric
        if (success && logCount > 0)
        {
            var logsPerSecond = logCount / duration.TotalSeconds;
            RecordMetric("CopilotStudio.LogsPerSecond", logsPerSecond, "logs/sec");
        }
    }

    private class OperationTimer : IDisposable
    {
        private readonly string _operationName;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public OperationTimer(string operationName, ILogger logger)
        {
            _operationName = operationName;
            _logger = logger;
            _stopwatch = Stopwatch.StartNew();
            
            _logger.LogDebug("Started operation: {OperationName}", _operationName);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _stopwatch.Stop();
                _logger.LogInformation("Completed operation: {OperationName} in {Duration}ms", 
                    _operationName, _stopwatch.ElapsedMilliseconds);
                
                _disposed = true;
            }
        }
    }
}