using System.Text.Json;
using ErrorLogPrioritization.Api.Models;

namespace ErrorLogPrioritization.Api.Utils
{
    public class JsonFileManager : IJsonFileManager
    {
        private readonly string _basePath;
        private readonly ILogger<JsonFileManager> _logger;

        public JsonFileManager(IConfiguration configuration, ILogger<JsonFileManager> logger)
        {
            _basePath = configuration["FileStorage:LogsPath"] ?? "Data/Logs";
            _logger = logger;
            
            // Ensure directory exists
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public async Task<List<ErrorLog>> LoadLogsAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_basePath, filePath);
                
                if (!File.Exists(fullPath))
                {
                    _logger.LogInformation("Log file {FilePath} does not exist, returning empty list", fullPath);
                    return new List<ErrorLog>();
                }

                var jsonContent = await File.ReadAllTextAsync(fullPath);
                
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    return new List<ErrorLog>();
                }

                var logs = JsonSerializer.Deserialize<List<ErrorLog>>(jsonContent);
                return logs ?? new List<ErrorLog>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading logs from file {FilePath}", filePath);
                return new List<ErrorLog>();
            }
        }

        public async Task SaveLogsAsync(string filePath, List<ErrorLog> logs)
        {
            try
            {
                var fullPath = Path.Combine(_basePath, filePath);
                var directory = Path.GetDirectoryName(fullPath);
                
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonContent = JsonSerializer.Serialize(logs, options);
                await File.WriteAllTextAsync(fullPath, jsonContent);
                
                _logger.LogInformation("Successfully saved {Count} logs to {FilePath}", logs.Count, fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving logs to file {FilePath}", filePath);
                throw;
            }
        }

        public async Task AppendLogAsync(string filePath, ErrorLog log)
        {
            try
            {
                var logs = await LoadLogsAsync(filePath);
                logs.Add(log);
                await SaveLogsAsync(filePath, logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error appending log to file {FilePath}", filePath);
                throw;
            }
        }

        public string GetDailyLogFilePath(DateTime date)
        {
            return $"logs-{date:yyyy-MM-dd}.json";
        }

        public async Task<List<ErrorLog>> LoadLogsByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            var allLogs = new List<ErrorLog>();
            var currentDate = fromDate.Date;

            while (currentDate <= toDate.Date)
            {
                var dailyFilePath = GetDailyLogFilePath(currentDate);
                var dailyLogs = await LoadLogsAsync(dailyFilePath);
                
                // Filter logs by time range within the day
                var filteredLogs = dailyLogs.Where(log => 
                    log.Timestamp >= fromDate && log.Timestamp <= toDate).ToList();
                
                allLogs.AddRange(filteredLogs);
                currentDate = currentDate.AddDays(1);
            }

            return allLogs;
        }

        public bool FileExists(string filePath)
        {
            var fullPath = Path.Combine(_basePath, filePath);
            return File.Exists(fullPath);
        }
    }
}