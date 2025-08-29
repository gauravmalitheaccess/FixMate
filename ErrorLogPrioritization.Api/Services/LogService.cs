using ErrorLogPrioritization.Api.Models;
using ErrorLogPrioritization.Api.Utils;

namespace ErrorLogPrioritization.Api.Services
{
    public interface ILogService
    {
        Task<bool> CollectLogsAsync(List<ErrorLog> logs);
        Task<List<ErrorLog>> GetFilteredLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, 
            string? severity = null, string? priority = null);
        Task<LogStatistics> GetLogStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<bool> UpdateLogResolutionStatusAsync(string logId, string resolutionStatus, DateTime resolvedAt, string resolvedBy);
    }

    public class LogService : ILogService
    {
        private readonly IJsonFileManager _jsonFileManager;
        private readonly ILogger<LogService> _logger;

        public LogService(IJsonFileManager jsonFileManager, ILogger<LogService> logger)
        {
            _jsonFileManager = jsonFileManager;
            _logger = logger;
        }

        public async Task<bool> CollectLogsAsync(List<ErrorLog> logs)
        {
            try
            {
                if (logs == null || !logs.Any())
                {
                    _logger.LogWarning("No logs provided for collection");
                    return false;
                }

                // Group logs by date for daily file storage
                var logsByDate = logs.GroupBy(log => log.Timestamp.Date);

                foreach (var dateGroup in logsByDate)
                {
                    var dailyFilePath = _jsonFileManager.GetDailyLogFilePath(dateGroup.Key);
                    var existingLogs = await _jsonFileManager.LoadLogsAsync(dailyFilePath);
                    
                    // Add new logs to existing ones
                    existingLogs.AddRange(dateGroup.ToList());
                    
                    // Save updated logs
                    await _jsonFileManager.SaveLogsAsync(dailyFilePath, existingLogs);
                    
                    _logger.LogInformation("Collected {Count} logs for date {Date}", 
                        dateGroup.Count(), dateGroup.Key.ToString("yyyy-MM-dd"));
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting logs");
                return false;
            }
        }

        public async Task<List<ErrorLog>> GetFilteredLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, 
            string? severity = null, string? priority = null)
        {
            try
            {
                // Set default date range if not provided (last 30 days)
                var endDate = toDate ?? DateTime.UtcNow;
                var startDate = fromDate ?? endDate.AddDays(-30);

                // Load logs from date range
                var logs = await _jsonFileManager.LoadLogsByDateRangeAsync(startDate, endDate);

                // Apply filters
                var filteredLogs = logs.AsQueryable();

                if (!string.IsNullOrEmpty(severity))
                {
                    filteredLogs = filteredLogs.Where(log => 
                        string.Equals(log.Severity, severity, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(priority))
                {
                    filteredLogs = filteredLogs.Where(log => 
                        string.Equals(log.Priority, priority, StringComparison.OrdinalIgnoreCase));
                }

                var result = filteredLogs.OrderByDescending(log => log.Timestamp).ToList();
                
                _logger.LogInformation("Retrieved {Count} filtered logs", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filtered logs");
                return new List<ErrorLog>();
            }
        }

        public async Task<LogStatistics> GetLogStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                // Set default date range if not provided (last 30 days)
                var endDate = toDate ?? DateTime.UtcNow;
                var startDate = fromDate ?? endDate.AddDays(-30);

                // Load logs from date range
                var logs = await _jsonFileManager.LoadLogsByDateRangeAsync(startDate, endDate);

                var severityBreakdown = logs
                    .Where(log => !string.IsNullOrEmpty(log.Severity))
                    .GroupBy(log => log.Severity)
                    .ToDictionary(g => g.Key, g => g.Count());

                var priorityBreakdown = logs
                    .Where(log => !string.IsNullOrEmpty(log.Priority))
                    .GroupBy(log => log.Priority)
                    .ToDictionary(g => g.Key, g => g.Count());

                var today = DateTime.UtcNow.Date;
                var weekAgo = today.AddDays(-7);
                var monthAgo = today.AddDays(-30);

                var statistics = new LogStatistics
                {
                    TotalLogs = logs.Count,
                    AnalyzedLogs = logs.Count(log => log.IsAnalyzed),
                    UnanalyzedLogs = logs.Count(log => !log.IsAnalyzed),
                    AnalyzedCount = logs.Count(log => log.IsAnalyzed),
                    UnanalyzedCount = logs.Count(log => !log.IsAnalyzed),
                    
                    // Severity counts
                    CriticalCount = severityBreakdown.GetValueOrDefault("Critical", 0),
                    HighCount = severityBreakdown.GetValueOrDefault("High", 0),
                    MediumCount = severityBreakdown.GetValueOrDefault("Medium", 0),
                    LowCount = severityBreakdown.GetValueOrDefault("Low", 0),
                    
                    // Priority counts
                    HighPriorityCount = priorityBreakdown.GetValueOrDefault("High", 0),
                    MediumPriorityCount = priorityBreakdown.GetValueOrDefault("Medium", 0),
                    LowPriorityCount = priorityBreakdown.GetValueOrDefault("Low", 0),
                    
                    // Time-based counts
                    TodayCount = logs.Count(log => log.Timestamp.Date == today),
                    WeekCount = logs.Count(log => log.Timestamp.Date >= weekAgo),
                    MonthCount = logs.Count(log => log.Timestamp.Date >= monthAgo),
                    
                    SeverityBreakdown = severityBreakdown,
                    PriorityBreakdown = priorityBreakdown,
                    DateRange = new DateRange
                    {
                        FromDate = startDate,
                        ToDate = endDate
                    },
                    LastUpdated = DateTime.UtcNow,
                    GeneratedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Generated statistics for {Count} logs", logs.Count);
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating log statistics");
                return new LogStatistics
                {
                    TotalLogs = 0,
                    AnalyzedLogs = 0,
                    UnanalyzedLogs = 0,
                    SeverityBreakdown = new Dictionary<string, int>(),
                    PriorityBreakdown = new Dictionary<string, int>(),
                    DateRange = new DateRange
                    {
                        FromDate = fromDate ?? DateTime.UtcNow.AddDays(-30),
                        ToDate = toDate ?? DateTime.UtcNow
                    },
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        public async Task<bool> UpdateLogResolutionStatusAsync(string logId, string resolutionStatus, DateTime resolvedAt, string resolvedBy)
        {
            try
            {
                if (string.IsNullOrEmpty(logId))
                {
                    _logger.LogWarning("Log ID cannot be empty for resolution status update");
                    return false;
                }

                // Load logs from a reasonable date range to find the log
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddDays(-90);
                
                var logs = await _jsonFileManager.LoadLogsByDateRangeAsync(startDate, endDate);
                
                var logToUpdate = logs.FirstOrDefault(log => log.Id == logId);
                
                if (logToUpdate == null)
                {
                    _logger.LogWarning("Log with ID {LogId} not found for resolution status update", logId);
                    return false;
                }

                logToUpdate.ResolutionStatus = resolutionStatus;
                logToUpdate.ResolvedAt = resolvedAt;
                logToUpdate.ResolvedBy = resolvedBy;

                var logDate = logToUpdate.Timestamp.Date;
                var dailyFilePath = _jsonFileManager.GetDailyLogFilePath(logDate);
                var dailyLogs = await _jsonFileManager.LoadLogsAsync(dailyFilePath);
                
                var dailyLogIndex = dailyLogs.FindIndex(log => log.Id == logId);
                if (dailyLogIndex >= 0)
                {
                    dailyLogs[dailyLogIndex] = logToUpdate;
                    await _jsonFileManager.SaveLogsAsync(dailyFilePath, dailyLogs);
                    
                    _logger.LogInformation("Successfully updated resolution status for log {LogId} to {Status}", 
                        logId, resolutionStatus);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Log {LogId} not found in daily file for date {Date}", logId, logDate);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating resolution status for log {LogId}", logId);
                return false;
            }
        }
    }
}
