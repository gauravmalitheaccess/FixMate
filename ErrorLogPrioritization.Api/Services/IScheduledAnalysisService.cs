using ErrorLogPrioritization.Api.Models;

namespace ErrorLogPrioritization.Api.Services;

public interface IScheduledAnalysisService
{
    Task ExecuteDailyAnalysisAsync();
    Task<List<ErrorLog>> CollectPreviousDayLogsAsync(DateTime date);
    Task TriggerCopilotAnalysisAsync(List<ErrorLog> logs);
}