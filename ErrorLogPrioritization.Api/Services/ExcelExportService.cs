using ErrorLogPrioritization.Api.Models;
using ClosedXML.Excel;

namespace ErrorLogPrioritization.Api.Services
{
    public class ExcelExportService : IExcelExportService
    {
        private readonly ILogger<ExcelExportService> _logger;

        public ExcelExportService(ILogger<ExcelExportService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> GenerateExcelReportAsync(List<ErrorLog> logs, DateTime reportDate)
        {
            try
            {
                _logger.LogInformation("Generating Excel report for {LogCount} logs on {ReportDate}", logs.Count, reportDate.ToString("yyyy-MM-dd"));

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Error Logs");

                // Add headers
                AddHeaders(worksheet);

                // Add data
                await AddDataAsync(worksheet, logs);

                // Apply formatting
                ApplyFormatting(worksheet, logs.Count);

                // Apply conditional formatting based on priority
                ApplyConditionalFormatting(worksheet, logs.Count);

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return await Task.FromResult(stream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel report for date {ReportDate}", reportDate);
                throw;
            }
        }

        public async Task<string> GenerateExcelFileAsync(List<ErrorLog> logs, DateTime reportDate, string outputDirectory)
        {
            try
            {
                var fileName = GetExcelFileName(reportDate);
                var filePath = Path.Combine(outputDirectory, fileName);

                // Ensure directory exists
                Directory.CreateDirectory(outputDirectory);

                var excelData = await GenerateExcelReportAsync(logs, reportDate);
                await File.WriteAllBytesAsync(filePath, excelData);

                _logger.LogInformation("Excel file generated successfully at {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel file for date {ReportDate}", reportDate);
                throw;
            }
        }

        public string GetExcelFileName(DateTime reportDate)
        {
            return $"error-logs-{reportDate:yyyy-MM-dd}.xlsx";
        }

        private void AddHeaders(IXLWorksheet worksheet)
        {
            var headers = new[]
            {
                "Priority",
                "Timestamp",
                "Source", 
                "Message",
                "Severity",
                "AI Reasoning",
                "Stack Trace",
                "Analyzed At"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }
        }

        private async Task AddDataAsync(IXLWorksheet worksheet, List<ErrorLog> logs)
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < logs.Count; i++)
                {
                    var log = logs[i];
                    var row = i + 2; // Start from row 2 (after headers)

                    worksheet.Cell(row, 1).Value = log.Priority;
                    worksheet.Cell(row, 2).Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cell(row, 3).Value = log.Source;
                    worksheet.Cell(row, 4).Value = log.Message;
                    worksheet.Cell(row, 5).Value = log.Severity;
                    worksheet.Cell(row, 6).Value = log.AiReasoning;
                    worksheet.Cell(row, 7).Value = log.StackTrace;
                    worksheet.Cell(row, 8).Value = log.AnalyzedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Not Analyzed";
                }
            });
        }

        private void ApplyFormatting(IXLWorksheet worksheet, int dataRowCount)
        {
            var totalRows = dataRowCount + 1; // +1 for header row

            // Header formatting
            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data formatting
            if (totalRows > 1)
            {
                var dataRange = worksheet.Range(2, 1, totalRows, 8);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            }

            // Set column widths
            worksheet.Column(1).Width = 20; // Timestamp
            worksheet.Column(2).Width = 25; // Source
            worksheet.Column(3).Width = 50; // Message
            worksheet.Column(4).Width = 15; // Severity
            worksheet.Column(5).Width = 15; // Priority
            worksheet.Column(6).Width = 40; // AI Reasoning
            worksheet.Column(7).Width = 60; // Stack Trace
            worksheet.Column(8).Width = 20; // Analyzed At

            // Wrap text for message and stack trace columns
            worksheet.Column(3).Style.Alignment.WrapText = true;
            worksheet.Column(6).Style.Alignment.WrapText = true;
            worksheet.Column(7).Style.Alignment.WrapText = true;
        }

        private void ApplyConditionalFormatting(IXLWorksheet worksheet, int dataRowCount)
        {
            if (dataRowCount == 0) return;

            var totalRows = dataRowCount + 1;

            // Priority column conditional formatting (column 5)
            for (int row = 2; row <= totalRows; row++)
            {
                var priorityCell = worksheet.Cell(row, 1);
                var priority = priorityCell.Value.ToString();

                switch (priority?.ToLower())
                {
                    case "high":
                        priorityCell.Style.Fill.BackgroundColor = XLColor.LightCoral;
                        priorityCell.Style.Font.Bold = true;
                        break;
                    case "medium":
                        priorityCell.Style.Fill.BackgroundColor = XLColor.Orange;
                        break;
                    case "low":
                        priorityCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        break;
                }

                // Severity column conditional formatting (column 4)
                var severityCell = worksheet.Cell(row, 5);
                var severity = severityCell.Value.ToString();

                switch (severity?.ToLower())
                {
                    case "critical":
                        severityCell.Style.Fill.BackgroundColor = XLColor.DarkRed;
                        severityCell.Style.Font.FontColor = XLColor.White;
                        severityCell.Style.Font.Bold = true;
                        break;
                    case "high":
                        severityCell.Style.Fill.BackgroundColor = XLColor.Red;
                        severityCell.Style.Font.FontColor = XLColor.White;
                        break;
                }
            }
        }
    }
}