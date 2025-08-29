using ErrorLogPrioritization.Api.Models;
using ErrorLogPrioritization.Api.Utils;
using Hangfire;

namespace ErrorLogPrioritization.Api.Services;

public class ScheduledAnalysisService : IScheduledAnalysisService
{
    private readonly IJsonFileManager _jsonFileManager;
    private readonly ICopilotStudioService _copilotStudioService;
    private readonly ILogger<ScheduledAnalysisService> _logger;
    private readonly IBackgroundJobClient? _backgroundJobClient;

    public ScheduledAnalysisService(
        IJsonFileManager jsonFileManager,
        ICopilotStudioService copilotStudioService,
        ILogger<ScheduledAnalysisService> logger,
        IBackgroundJobClient? backgroundJobClient = null)
    {
        _jsonFileManager = jsonFileManager;
        _copilotStudioService = copilotStudioService;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task ExecuteDailyAnalysisAsync()
    {
        try
        {
            _logger.LogInformation("Starting daily analysis at {Timestamp}", DateTime.UtcNow);

            var previousDay = DateTime.UtcNow.Date.AddDays(-1);
            var logs = await CollectPreviousDayLogsAsync(previousDay);

            if (!logs.Any())
            {
                _logger.LogInformation("No logs found for {Date}. Skipping analysis.", previousDay.ToString("yyyy-MM-dd"));
                return;
            }

            _logger.LogInformation("Found {LogCount} logs for analysis on {Date}", logs.Count, previousDay.ToString("yyyy-MM-dd"));

            await TriggerCopilotAnalysisAsync(logs);

            _logger.LogInformation("Daily analysis completed successfully at {Timestamp}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during daily analysis execution");
            
            // Schedule retry in 30 minutes if background job client is available
            if (_backgroundJobClient != null)
            {
                _backgroundJobClient.Schedule<IScheduledAnalysisService>(
                    service => service.ExecuteDailyAnalysisAsync(),
                    TimeSpan.FromMinutes(30));
            }
            
            throw;
        }
    }

    public async Task<List<ErrorLog>> CollectPreviousDayLogsAsync(DateTime date)
    {
        try
        {
            _logger.LogInformation("Collecting logs for date: {Date}", date.ToString("yyyy-MM-dd"));

            var filePath = _jsonFileManager.GetDailyLogFilePath(date);
            
            if (!_jsonFileManager.FileExists(filePath))
            {
                _logger.LogWarning("Log file not found for date {Date}: {FilePath}", date.ToString("yyyy-MM-dd"), filePath);
                return new List<ErrorLog>();
            }

            var logs = await _jsonFileManager.LoadLogsAsync(filePath);
            
            // Filter logs that haven't been analyzed yet
            var unanalyzedLogs = logs.Where(log => !log.IsAnalyzed).ToList();
            
            _logger.LogInformation("Loaded {TotalLogs} logs, {UnanalyzedLogs} unanalyzed for date {Date}", 
                logs.Count, unanalyzedLogs.Count, date.ToString("yyyy-MM-dd"));

            return unanalyzedLogs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting logs for date {Date}", date.ToString("yyyy-MM-dd"));
            throw;
        }
    }

    public async Task TriggerCopilotAnalysisAsync(List<ErrorLog> logs)
    {
        try
        {
            _logger.LogInformation("Triggering Copilot Studio analysis for {LogCount} logs", logs.Count);

            // Get historical context from previous analysis results
            var historicalContext = await GetHistoricalContextAsync();

            _logger.LogInformation("Built historical context with {HistoricalLogCount} previous logs and {PatternCount} error patterns", 
                historicalContext?.PreviousAnalysisResults?.Count ?? 0,
                historicalContext?.ErrorPatterns?.Count ?? 0);

            // Perform the analysis with historical context
            var analysisResponse = await _copilotStudioService.AnalyzeLogsAsync(logs, historicalContext);
            
            if (analysisResponse != null)
            {
                _logger.LogInformation("Copilot Studio analysis completed successfully with {AnalyzedCount} analyzed logs", 
                    analysisResponse.AnalyzedLogs?.Count ?? 0);

                // Process and update logs with analysis results
                var analyzedLogs = await _copilotStudioService.ProcessAnalysisResultsAsync(logs, analysisResponse);
                
                // Save the updated logs back to the JSON file
                var currentDate = DateTime.UtcNow.Date.AddDays(-1);
                var filePath = _jsonFileManager.GetDailyLogFilePath(currentDate);
                await _copilotStudioService.UpdateLogsWithAnalysisAsync(analyzedLogs, filePath);
                
                _logger.LogInformation("Successfully updated {UpdatedLogCount} logs with analysis results", analyzedLogs.Count);
            }
            else
            {
                _logger.LogWarning("Copilot Studio analysis returned null response");
            }
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Copilot Studio analysis timed out after configured timeout period");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred during Copilot Studio analysis");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Copilot Studio analysis");
            throw;
        }
    }

    private async Task<HistoricalContext?> GetHistoricalContextAsync()
    {
        try
        {
            // Get logs from the past 7 days for historical context
            var endDate = DateTime.UtcNow.Date.AddDays(-1);
            var startDate = endDate.AddDays(-7);
            
            var historicalLogs = await _jsonFileManager.LoadLogsByDateRangeAsync(startDate, endDate);
            
            // Filter to only analyzed logs for context
            var analyzedLogs = historicalLogs.Where(log => log.IsAnalyzed).ToList();
            
            if (!analyzedLogs.Any())
            {
                _logger.LogInformation("No historical analyzed logs found for context");
                return null;
            }

            // Group by error patterns and priority levels for context
            var errorPatterns = analyzedLogs
                .GroupBy(log => new { log.Message, log.Priority })
                .Select(group => new
                {
                    Pattern = group.Key.Message,
                    Priority = group.Key.Priority,
                    Frequency = group.Count(),
                    LastOccurrence = group.Max(log => log.Timestamp)
                })
                .OrderByDescending(pattern => pattern.Frequency)
                .Take(20) // Limit to top 20 patterns
                .ToList();

            var context = new HistoricalContext
            {
                PreviousAnalysisResults = analyzedLogs.Take(100).ToList(), // Limit to recent 100 logs
                ErrorPatterns = errorPatterns.Select(p => new ErrorPattern
                {
                    Message = p.Pattern,
                    Priority = p.Priority,
                    Frequency = p.Frequency,
                    LastOccurrence = p.LastOccurrence
                }).ToList(),
                AnalysisDate = DateTime.UtcNow
            };

            _logger.LogInformation("Built historical context with {LogCount} previous logs and {PatternCount} error patterns", 
                context.PreviousAnalysisResults.Count, context.ErrorPatterns.Count);

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to build historical context, proceeding without context");
            return null;
        }
    }
}