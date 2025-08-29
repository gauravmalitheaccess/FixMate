using ErrorLogPrioritization.Api.Models;

namespace ErrorLogPrioritization.Api.Utils
{
    public interface IJsonFileManager
    {
        Task<List<ErrorLog>> LoadLogsAsync(string filePath);
        Task SaveLogsAsync(string filePath, List<ErrorLog> logs);
        Task AppendLogAsync(string filePath, ErrorLog log);
        string GetDailyLogFilePath(DateTime date);
        Task<List<ErrorLog>> LoadLogsByDateRangeAsync(DateTime fromDate, DateTime toDate);
        bool FileExists(string filePath);
    }
}