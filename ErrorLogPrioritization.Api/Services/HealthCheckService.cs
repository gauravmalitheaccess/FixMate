using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ErrorLogPrioritization.Api.Services;

public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckFileSystemHealthAsync();
    Task<HealthCheckResult> CheckCopilotStudioHealthAsync();
}

public class HealthCheckService : IHealthCheckService
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public HealthCheckService(ILogger<HealthCheckService> logger, IConfiguration configuration, HttpClient httpClient)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<HealthCheckResult> CheckFileSystemHealthAsync()
    {
        try
        {
            var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            
            // Check if logs directory exists, create if not
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            // Test write access
            var testFile = Path.Combine(logsDirectory, $"health-check-{Guid.NewGuid()}.tmp");
            await File.WriteAllTextAsync(testFile, "health check");
            
            // Test read access
            var content = await File.ReadAllTextAsync(testFile);
            
            // Clean up
            File.Delete(testFile);

            if (content == "health check")
            {
                return HealthCheckResult.Healthy("File system is accessible");
            }

            return HealthCheckResult.Degraded("File system access is limited");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File system health check failed");
            return HealthCheckResult.Unhealthy("File system is not accessible", ex);
        }
    }

    public async Task<HealthCheckResult> CheckCopilotStudioHealthAsync()
    {
        try
        {
            var copilotStudioUrl = _configuration["CopilotStudio:BaseUrl"];
            if (string.IsNullOrEmpty(copilotStudioUrl))
            {
                return HealthCheckResult.Degraded("Copilot Studio URL not configured");
            }

            // Simple connectivity check with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.GetAsync($"{copilotStudioUrl}/health", cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Copilot Studio is accessible");
            }

            return HealthCheckResult.Degraded($"Copilot Studio returned status: {response.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            return HealthCheckResult.Degraded("Copilot Studio health check timed out");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Copilot Studio health check failed");
            return HealthCheckResult.Degraded("Copilot Studio is not accessible", ex);
        }
    }
}

public class FileSystemHealthCheck : IHealthCheck
{
    private readonly IHealthCheckService _healthCheckService;

    public FileSystemHealthCheck(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return await _healthCheckService.CheckFileSystemHealthAsync();
    }
}

public class CopilotStudioHealthCheck : IHealthCheck
{
    private readonly IHealthCheckService _healthCheckService;

    public CopilotStudioHealthCheck(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return await _healthCheckService.CheckCopilotStudioHealthAsync();
    }
}