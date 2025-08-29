using ErrorLogPrioritization.Api.Models;

namespace ErrorLogPrioritization.Api.Services
{
    public interface IExcelExportService
    {
        Task<byte[]> GenerateExcelReportAsync(List<ErrorLog> logs, DateTime reportDate);
        Task<string> GenerateExcelFileAsync(List<ErrorLog> logs, DateTime reportDate, string outputDirectory);
        string GetExcelFileName(DateTime reportDate);
    }
}