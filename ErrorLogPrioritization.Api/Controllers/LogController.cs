using ErrorLogPrioritization.Api.Models;
using ErrorLogPrioritization.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace ErrorLogPrioritization.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Error Log Management", "Operations for collecting, retrieving, and exporting error logs")]
    [Produces("application/json")]
    public class LogController : ControllerBase
    {
        private readonly ILogService _logService;
        private readonly IExcelExportService _excelExportService;
        private readonly ILogger<LogController> _logger;

        public LogController(ILogService logService, IExcelExportService excelExportService, ILogger<LogController> logger)
        {
            _logService = logService;
            _excelExportService = excelExportService;
            _logger = logger;
        }

        /// <summary>
        /// Collect error logs from web applications
        /// </summary>
        /// <param name="logs">List of error logs to collect</param>
        /// <returns>Success status</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/log/collect
        ///     [
        ///         {
        ///             "id": "unique-log-id-123",
        ///             "timestamp": "2024-01-15T10:30:00Z",
        ///             "source": "MyApp.Controllers.UserController",
        ///             "message": "User authentication failed",
        ///             "stackTrace": "at UserController.Login() line 45",
        ///             "severity": "High"
        ///         }
        ///     ]
        /// 
        /// </remarks>
        [HttpPost("collect")]
        [SwaggerOperation(
            Summary = "Collect error logs",
            Description = "Accepts a collection of error logs from web applications for storage and analysis",
            OperationId = "CollectLogs",
            Tags = new[] { "Log Collection" }
        )]
        [SwaggerResponse(200, "Logs collected successfully", typeof(object))]
        [SwaggerResponse(400, "Invalid request data", typeof(object))]
        [SwaggerResponse(500, "Internal server error", typeof(object))]
        public async Task<IActionResult> CollectLogs([FromBody] List<ErrorLog> logs)
        {
            try
            {
                if (logs == null)
                {
                    _logger.LogWarning("Received null logs collection");
                    return BadRequest(new { error = "Logs collection cannot be null" });
                }

                if (!logs.Any())
                {
                    _logger.LogWarning("Received empty logs collection");
                    return BadRequest(new { error = "Logs collection cannot be empty" });
                }

                // Validate each log entry
                var validationErrors = new List<string>();
                for (int i = 0; i < logs.Count; i++)
                {
                    var log = logs[i];
                    var validationContext = new ValidationContext(log);
                    var validationResults = new List<ValidationResult>();
                    
                    if (!Validator.TryValidateObject(log, validationContext, validationResults, true))
                    {
                        foreach (var validationResult in validationResults)
                        {
                            validationErrors.Add($"Log {i}: {validationResult.ErrorMessage}");
                        }
                    }
                }

                if (validationErrors.Any())
                {
                    _logger.LogWarning("Validation errors in logs collection: {Errors}", string.Join(", ", validationErrors));
                    return BadRequest(new { error = "Validation errors", details = validationErrors });
                }

                var success = await _logService.CollectLogsAsync(logs);
                
                if (success)
                {
                    _logger.LogInformation("Successfully collected {Count} logs", logs.Count);
                    return Ok(new { message = "Logs collected successfully", count = logs.Count });
                }
                else
                {
                    _logger.LogError("Failed to collect logs");
                    return StatusCode(500, new { error = "Failed to collect logs" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while collecting logs");
                return StatusCode(500, new { error = "Internal server error occurred while collecting logs" });
            }
        }

        /// <summary>
        /// Retrieve filtered error logs
        /// </summary>
        /// <param name="fromDate">Start date for filtering (optional)</param>
        /// <param name="toDate">End date for filtering (optional)</param>
        /// <param name="severity">Severity level filter (optional): Critical, High, Medium, Low</param>
        /// <param name="priority">Priority level filter (optional): High, Medium, Low</param>
        /// <param name="page">Page number for pagination (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 50, max: 1000)</param>
        /// <returns>Filtered list of error logs with pagination information</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/log?fromDate=2024-01-01&amp;toDate=2024-01-31&amp;severity=High&amp;page=1&amp;pageSize=50
        /// 
        /// </remarks>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get filtered error logs",
            Description = "Retrieves error logs with optional filtering by date range, severity, and priority. Supports pagination.",
            OperationId = "GetLogs",
            Tags = new[] { "Log Retrieval" }
        )]
        [SwaggerResponse(200, "Successfully retrieved logs", typeof(object))]
        [SwaggerResponse(400, "Invalid request parameters", typeof(object))]
        [SwaggerResponse(500, "Internal server error", typeof(object))]
        public async Task<IActionResult> GetLogs(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? severity = null,
            [FromQuery] string? priority = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // Validate pagination parameters
                if (page < 1)
                {
                    return BadRequest(new { error = "Page number must be greater than 0" });
                }

                if (pageSize < 1 || pageSize > 1000)
                {
                    return BadRequest(new { error = "Page size must be between 1 and 1000" });
                }

                // Validate date range
                if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
                {
                    return BadRequest(new { error = "From date cannot be greater than to date" });
                }

                var logs = await _logService.GetFilteredLogsAsync(fromDate, toDate, severity, priority);
                
                // Apply pagination
                var totalCount = logs.Count;
                var paginatedLogs = logs
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var response = new
                {
                    logs = paginatedLogs,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    },
                    filters = new
                    {
                        fromDate = fromDate,
                        toDate = toDate,
                        severity = severity,
                        priority = priority
                    }
                };

                _logger.LogInformation("Retrieved {Count} logs (page {Page} of {TotalPages})", 
                    paginatedLogs.Count, page, response.pagination.totalPages);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving logs");
                return StatusCode(500, new { error = "Internal server error occurred while retrieving logs" });
            }
        }

        /// <summary>
        /// Get dashboard statistics for error logs
        /// </summary>
        /// <param name="fromDate">Start date for statistics (optional)</param>
        /// <param name="toDate">End date for statistics (optional)</param>
        /// <returns>Comprehensive log statistics for dashboard display</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/log/stats?fromDate=2024-01-01&amp;toDate=2024-01-31
        /// 
        /// Returns statistics including total logs, severity breakdown, priority distribution, and analysis status.
        /// </remarks>
        [HttpGet("stats")]
        [SwaggerOperation(
            Summary = "Get log statistics",
            Description = "Retrieves comprehensive statistics about error logs for dashboard display, including counts by severity, priority, and analysis status.",
            OperationId = "GetLogStatistics",
            Tags = new[] { "Dashboard" }
        )]
        [SwaggerResponse(200, "Successfully retrieved statistics", typeof(LogStatistics))]
        [SwaggerResponse(400, "Invalid date range", typeof(object))]
        [SwaggerResponse(500, "Internal server error", typeof(object))]
        public async Task<IActionResult> GetLogStatistics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                // Validate date range
                if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
                {
                    return BadRequest(new { error = "From date cannot be greater than to date" });
                }

                var statistics = await _logService.GetLogStatisticsAsync(fromDate, toDate);
                
                _logger.LogInformation("Generated statistics for {TotalLogs} logs", statistics.TotalLogs);
                
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating log statistics");
                return StatusCode(500, new { error = "Internal server error occurred while generating statistics" });
            }
        }

        /// <summary>
        /// Export error logs to Excel file for a specific date
        /// </summary>
        /// <param name="date">Date for which to export logs (format: yyyy-MM-dd)</param>
        /// <returns>Excel file download</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/log/export/2024-01-15
        /// 
        /// Downloads an Excel file containing all error logs for the specified date.
        /// The file includes columns for timestamp, source, message, severity, priority, and AI analysis results.
        /// </remarks>
        [HttpGet("export/{date}")]
        [SwaggerOperation(
            Summary = "Export logs to Excel for specific date",
            Description = "Generates and downloads an Excel file containing all error logs for the specified date.",
            OperationId = "ExportLogsToExcel",
            Tags = new[] { "Excel Export" }
        )]
        [SwaggerResponse(200, "Excel file generated successfully", typeof(FileResult))]
        [SwaggerResponse(400, "Invalid date format or future date", typeof(object))]
        [SwaggerResponse(404, "No logs found for the specified date", typeof(object))]
        [SwaggerResponse(500, "Internal server error", typeof(object))]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> ExportLogsToExcel([FromRoute] string date)
        {
            try
            {
                // Parse and validate the date parameter
                if (!DateTime.TryParseExact(date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime exportDate))
                {
                    _logger.LogWarning("Invalid date format provided: {Date}", date);
                    return BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd format." });
                }

                // Validate date is not in the future
                if (exportDate.Date > DateTime.Now.Date)
                {
                    _logger.LogWarning("Future date provided for export: {Date}", exportDate);
                    return BadRequest(new { error = "Cannot export logs for future dates." });
                }

                // Get logs for the specified date
                var fromDate = exportDate.Date;
                var toDate = exportDate.Date.AddDays(1).AddTicks(-1); // End of day
                
                var logs = await _logService.GetFilteredLogsAsync(fromDate, toDate, null, null);
                
                if (!logs.Any())
                {
                    _logger.LogInformation("No logs found for date: {Date}", exportDate.ToString("yyyy-MM-dd"));
                    return NotFound(new { error = $"No logs found for date {exportDate:yyyy-MM-dd}" });
                }

                // Generate Excel file
                var excelData = await _excelExportService.GenerateExcelReportAsync(logs, exportDate);
                var fileName = _excelExportService.GetExcelFileName(exportDate);

                _logger.LogInformation("Generated Excel export for {Date} with {Count} logs", 
                    exportDate.ToString("yyyy-MM-dd"), logs.Count);

                // Return file with proper headers
                return File(
                    excelData,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while exporting logs to Excel for date: {Date}", date);
                return StatusCode(500, new { error = "Internal server error occurred while generating Excel export" });
            }
        }

        /// <summary>
        /// Generate sample exception logs for testing purposes
        /// </summary>
        /// <param name="count">Number of sample logs to generate (default: 10, max: 100)</param>
        /// <param name="severity">Specific severity level for all generated logs (optional): Critical, High, Medium, Low</param>
        /// <param name="hoursBack">Generate logs from this many hours ago to now (default: 24)</param>
        /// <returns>Success status with generated logs count</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/log/generate?count=25&amp;severity=High&amp;hoursBack=48
        /// 
        /// Generates realistic sample error logs with various sources, messages, and stack traces.
        /// Useful for testing the dashboard and demonstrating the system capabilities.
        /// </remarks>
        [HttpPost("generate")]
        [SwaggerOperation(
            Summary = "Generate sample exception logs",
            Description = "Creates realistic sample error logs for testing and demonstration purposes. Logs are distributed across the specified time range.",
            OperationId = "GenerateSampleLogs",
            Tags = new[] { "Testing" }
        )]
        [SwaggerResponse(200, "Sample logs generated successfully", typeof(object))]
        [SwaggerResponse(400, "Invalid parameters", typeof(object))]
        [SwaggerResponse(500, "Internal server error", typeof(object))]
        public async Task<IActionResult> GenerateSampleLogs(
            [FromQuery] int count = 10,
            [FromQuery] string? severity = null,
            [FromQuery] int hoursBack = 24)
        {
            try
            {
                // Validate parameters
                if (count < 1 || count > 100)
                {
                    return BadRequest(new { error = "Count must be between 1 and 100" });
                }

                if (hoursBack < 1 || hoursBack > 168) // Max 1 week
                {
                    return BadRequest(new { error = "Hours back must be between 1 and 168 (1 week)" });
                }

                // Validate severity if provided
                var validSeverities = new[] { "Critical", "High", "Medium", "Low" };
                if (!string.IsNullOrEmpty(severity) && !validSeverities.Contains(severity, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new { error = $"Invalid severity. Must be one of: {string.Join(", ", validSeverities)}" });
                }

                // Generate sample logs
                var sampleLogs = GenerateSampleErrorLogs(count, severity, hoursBack);
                
                // Save the generated logs
                var success = await _logService.CollectLogsAsync(sampleLogs);
                
                if (success)
                {
                    _logger.LogInformation("Successfully generated and saved {Count} sample logs", sampleLogs.Count);
                    return Ok(new 
                    { 
                        message = "Sample logs generated successfully", 
                        count = sampleLogs.Count,
                        timeRange = new
                        {
                            from = DateTime.UtcNow.AddHours(-hoursBack),
                            to = DateTime.UtcNow
                        },
                        severity = severity ?? "Mixed"
                    });
                }
                else
                {
                    _logger.LogError("Failed to save generated sample logs");
                    return StatusCode(500, new { error = "Failed to save generated sample logs" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating sample logs");
                return StatusCode(500, new { error = "Internal server error occurred while generating sample logs" });
            }
        }

        /// <summary>
        /// Export error logs to Excel file for a date range
        /// </summary>
        /// <param name="fromDate">Start date for export (format: yyyy-MM-dd)</param>
        /// <param name="toDate">End date for export (format: yyyy-MM-dd, optional - defaults to fromDate)</param>
        /// <returns>Excel file download</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/log/export?fromDate=2024-01-01&amp;toDate=2024-01-07
        /// 
        /// Downloads an Excel file containing all error logs for the specified date range.
        /// Maximum date range is 31 days to prevent excessive data exports.
        /// The file includes comprehensive log data with AI analysis results.
        /// </remarks>
        [HttpGet("export")]
        [SwaggerOperation(
            Summary = "Export logs to Excel for date range",
            Description = "Generates and downloads an Excel file containing all error logs for the specified date range (maximum 31 days).",
            OperationId = "ExportLogsToExcelRange",
            Tags = new[] { "Excel Export" }
        )]
        [SwaggerResponse(200, "Excel file generated successfully", typeof(FileResult))]
        [SwaggerResponse(400, "Invalid date format, future date, or date range exceeds 31 days", typeof(object))]
        [SwaggerResponse(404, "No logs found for the specified date range", typeof(object))]
        [SwaggerResponse(500, "Internal server error", typeof(object))]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> ExportLogsToExcelRange(
            [FromQuery] string fromDate,
            [FromQuery] string? toDate = null)
        {
            try
            {
                // Parse and validate the fromDate parameter
                if (string.IsNullOrEmpty(fromDate) || 
                    !DateTime.TryParseExact(fromDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime startDate))
                {
                    _logger.LogWarning("Invalid fromDate format provided: {FromDate}", fromDate);
                    return BadRequest(new { error = "Invalid fromDate format. Use yyyy-MM-dd format." });
                }

                // Parse toDate or default to fromDate
                DateTime endDate = startDate;
                if (!string.IsNullOrEmpty(toDate))
                {
                    if (!DateTime.TryParseExact(toDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out endDate))
                    {
                        _logger.LogWarning("Invalid toDate format provided: {ToDate}", toDate);
                        return BadRequest(new { error = "Invalid toDate format. Use yyyy-MM-dd format." });
                    }
                }

                // Validate date range
                if (startDate > endDate)
                {
                    return BadRequest(new { error = "fromDate cannot be greater than toDate." });
                }

                // Validate dates are not in the future
                if (startDate.Date > DateTime.Now.Date || endDate.Date > DateTime.Now.Date)
                {
                    _logger.LogWarning("Future date provided for export: {StartDate} to {EndDate}", startDate, endDate);
                    return BadRequest(new { error = "Cannot export logs for future dates." });
                }

                // Limit date range to prevent excessive data exports
                if ((endDate - startDate).TotalDays > 31)
                {
                    return BadRequest(new { error = "Date range cannot exceed 31 days." });
                }

                // Get logs for the specified date range
                var fromDateTime = startDate.Date;
                var toDateTime = endDate.Date.AddDays(1).AddTicks(-1); // End of day
                
                var logs = await _logService.GetFilteredLogsAsync(fromDateTime, toDateTime, null, null);
                
                if (!logs.Any())
                {
                    _logger.LogInformation("No logs found for date range: {StartDate} to {EndDate}", 
                        startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                    return NotFound(new { error = $"No logs found for date range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}" });
                }

                // Generate Excel file
                var reportDate = startDate == endDate ? startDate : startDate; // Use start date for filename
                var excelData = await _excelExportService.GenerateExcelReportAsync(logs, reportDate);
                
                // Create filename for date range
                var fileName = startDate == endDate 
                    ? _excelExportService.GetExcelFileName(startDate)
                    : $"error-logs-{startDate:yyyy-MM-dd}-to-{endDate:yyyy-MM-dd}.xlsx";

                _logger.LogInformation("Generated Excel export for date range {StartDate} to {EndDate} with {Count} logs", 
                    startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"), logs.Count);

                // Return file with proper headers
                return File(
                    excelData,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while exporting logs to Excel for date range: {FromDate} to {ToDate}", 
                    fromDate, toDate);
                return StatusCode(500, new { error = "Internal server error occurred while generating Excel export" });
            }
        }

        /// <summary>
        /// Generates realistic sample error logs for testing purposes
        /// </summary>
        private List<ErrorLog> GenerateSampleErrorLogs(int count, string? severity, int hoursBack)
        {
            var random = new Random();
            var logs = new List<ErrorLog>();
            var now = DateTime.UtcNow;
            var startTime = now.AddHours(-hoursBack);

            // Sample data for realistic error logs
            var sources = new[]
            {
                "UserController.Login",
                "OrderController.ProcessPayment",
                "ProductController.GetProduct",
                "AuthenticationService.ValidateToken",
                "DatabaseService.ExecuteQuery",
                "EmailService.SendNotification",
                "FileUploadController.UploadFile",
                "ReportController.GenerateReport",
                "ApiGateway.RouteRequest",
                "CacheService.GetCachedData",
                "PaymentService.ProcessTransaction",
                "InventoryService.UpdateStock",
                "UserService.CreateAccount",
                "OrderService.CalculateTotal",
                "NotificationService.SendPush"
            };

            var errorMessages = new[]
            {
                "Object reference not set to an instance of an object",
                "The connection string property has not been initialized",
                "Index was outside the bounds of the array",
                "Unable to cast object of type 'System.String' to type 'System.Int32'",
                "The operation has timed out",
                "Access to the path is denied",
                "A network-related or instance-specific error occurred",
                "The process cannot access the file because it is being used by another process",
                "Value cannot be null. Parameter name: userId",
                "The remote server returned an error: (404) Not Found",
                "Arithmetic operation resulted in an overflow",
                "The given key was not present in the dictionary",
                "Unable to connect to the remote server",
                "The request was aborted: The request was canceled",
                "Invalid operation. The connection is closed"
            };

            var stackTraces = new[]
            {
                "   at System.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection)\n   at System.Data.SqlClient.TdsParser.ThrowExceptionAndWarning()\n   at System.Data.SqlClient.TdsParser.Run(RunBehavior runBehavior, SqlCommand cmdHandler)",
                "   at System.Collections.Generic.Dictionary`2.get_Item(TKey key)\n   at MyApp.Services.UserService.GetUser(Int32 userId) in C:\\Source\\MyApp\\Services\\UserService.cs:line 45\n   at MyApp.Controllers.UserController.GetUserProfile(Int32 id)",
                "   at System.IO.FileStream.Init(String path, FileMode mode, FileAccess access, Int32 rights, Boolean useRights, FileShare share, Int32 bufferSize, FileOptions options)\n   at System.IO.FileStream..ctor(String path, FileMode mode, FileAccess access, FileShare share)",
                "   at System.Net.HttpWebRequest.GetResponse()\n   at MyApp.Services.ApiService.MakeRequest(String url) in C:\\Source\\MyApp\\Services\\ApiService.cs:line 78\n   at MyApp.Controllers.DataController.FetchExternalData()",
                "   at System.Threading.Tasks.Task.ThrowIfExceptional(Boolean includeTaskCanceledExceptions)\n   at System.Threading.Tasks.Task.Wait(Int32 millisecondsTimeout, CancellationToken cancellationToken)\n   at MyApp.Services.BackgroundService.ProcessQueue()",
                "   at System.Convert.ToInt32(String value)\n   at MyApp.Models.Order.CalculateTotal() in C:\\Source\\MyApp\\Models\\Order.cs:line 123\n   at MyApp.Controllers.OrderController.ProcessOrder(OrderRequest request)",
                "   at Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerHandler.HandleAuthenticateAsync()\n   at Microsoft.AspNetCore.Authentication.AuthenticationHandler`1.AuthenticateAsync()\n   at Microsoft.AspNetCore.Authentication.AuthenticationService.AuthenticateAsync()",
                "   at System.Data.Entity.Internal.LazyInternalContext.InitializeContext()\n   at System.Data.Entity.Internal.InternalContext.GetEntitySetAndBaseTypeForType(Type entityType)\n   at System.Data.Entity.Internal.Linq.InternalSet`1.Initialize()",
                "   at System.Web.Http.Controllers.ReflectedHttpActionDescriptor.ActionExecutor.<>c__DisplayClass10.<GetExecutor>b__9(Object instance, Object[] methodParameters)\n   at System.Web.Http.Controllers.ReflectedHttpActionDescriptor.ExecuteAsync()",
                "   at Newtonsoft.Json.JsonConvert.DeserializeObject[T](String value, JsonSerializerSettings settings)\n   at MyApp.Services.JsonService.ParseResponse[T](String json) in C:\\Source\\MyApp\\Services\\JsonService.cs:line 34"
            };

            var severities = string.IsNullOrEmpty(severity) 
                ? new[] { "Critical", "High", "Medium", "Low" }
                : new[] { severity };

            for (int i = 0; i < count; i++)
            {
                // Generate random timestamp within the specified range
                var randomHours = random.NextDouble() * hoursBack;
                var timestamp = startTime.AddHours(randomHours);

                // Select random data
                var selectedSeverity = severities[random.Next(severities.Length)];
                var source = sources[random.Next(sources.Length)];
                var message = errorMessages[random.Next(errorMessages.Length)];
                var stackTrace = stackTraces[random.Next(stackTraces.Length)];

                var log = new ErrorLog
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = timestamp,
                    Source = source,
                    Message = message,
                    StackTrace = stackTrace,
                    Severity = selectedSeverity,
                    Priority = string.Empty, // Will be set by AI analysis
                    AiReasoning = string.Empty,
                    PotentialFix = string.Empty,
                    AnalyzedAt = null,
                    IsAnalyzed = false
                };

                logs.Add(log);
            }

            return logs.OrderBy(l => l.Timestamp).ToList();
        }

        /// <summary>
        /// Analyze raw exception data using AI and return enhanced error logs
        /// </summary>
        /// <param name="rawExceptions">List of raw exception data from applications</param>
        /// <returns>AI-analyzed error logs with severity, priority, and reasoning</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/log/analyze
        ///     [
        ///         {
        ///             "source": "CacheService.GetCachedData",
        ///             "message": "Unable to connect to the remote server",
        ///             "stackTrace": "   at System.Net.HttpWebRequest.GetResponse()..."
        ///         }
        ///     ]
        /// 
        /// Returns enhanced logs with AI analysis including severity, priority, and reasoning.
        /// </remarks>
        [HttpPost("analyze")]
        [SwaggerOperation(
            Summary = "Analyze raw exceptions with AI",
            Description = "Accepts raw exception data and returns AI-enhanced error logs with severity, priority, and reasoning analysis.",
            OperationId = "AnalyzeExceptions",
            Tags = new[] { "AI Analysis" }
        )]
        [SwaggerResponse(200, "Exceptions analyzed successfully", typeof(List<ErrorLog>))]
        [SwaggerResponse(400, "Invalid request data", typeof(object))]
        [SwaggerResponse(500, "Internal server error", typeof(object))]
        public async Task<IActionResult> AnalyzeExceptions([FromBody] List<RawExceptionData> rawExceptions)
        {
            try
            {
                if (rawExceptions == null || !rawExceptions.Any())
                {
                    _logger.LogWarning("Received null or empty raw exceptions collection");
                    return BadRequest(new { error = "Raw exceptions collection cannot be null or empty" });
                }

                // Convert raw exceptions to ErrorLog objects
                var errorLogs = rawExceptions.Select(raw => new ErrorLog
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow,
                    Source = raw.Source ?? "Unknown",
                    Message = raw.Message ?? "Unknown error",
                    StackTrace = raw.StackTrace ?? "",
                    Severity = "Unknown", // Will be determined by AI
                    Priority = "",
                    AiReasoning = "",
                    PotentialFix = "",
                    AnalyzedAt = null,
                    IsAnalyzed = false
                }).ToList();

                // Get the CopilotStudioService from DI container
                var copilotService = HttpContext.RequestServices.GetService<ICopilotStudioService>();
                
                if (copilotService != null)
                {
                    try
                    {
                        // Analyze logs using AI
                        _logger.LogInformation("Sending {Count} logs for AI analysis", errorLogs.Count);
                        var analysisResponse = await copilotService.AnalyzeLogsAsync(errorLogs);
                        
                        if (analysisResponse != null)
                        {
                            // Process AI analysis results
                            var analyzedLogs = await copilotService.ProcessAnalysisResultsAsync(errorLogs, analysisResponse);
                            
                            // Save the analyzed logs
                            var success = await _logService.CollectLogsAsync(analyzedLogs);
                            
                            if (success)
                            {
                                _logger.LogInformation("Successfully analyzed and saved {Count} logs", analyzedLogs.Count);
                                return Ok(analyzedLogs);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to save analyzed logs, returning analysis results only");
                                return Ok(analyzedLogs);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("AI analysis returned null response, using basic severity assignment");
                            // Fallback: assign basic severity based on keywords
                            await AssignBasicSeverityWithAiFixesAsync(errorLogs);
                        }
                    }
                    catch (Exception aiEx)
                    {
                        _logger.LogError(aiEx, "Error during AI analysis, falling back to basic severity assignment");
                        // Fallback: assign basic severity based on keywords
                        await AssignBasicSeverityWithAiFixesAsync(errorLogs);
                    }
                }
                else
                {
                    _logger.LogWarning("CopilotStudioService not available, using basic severity assignment");
                    // Fallback: assign basic severity based on keywords
                    await AssignBasicSeverityWithAiFixesAsync(errorLogs);
                }

                // Save the logs (with or without AI analysis)
                var saveSuccess = await _logService.CollectLogsAsync(errorLogs);
                
                if (saveSuccess)
                {
                    _logger.LogInformation("Successfully processed and saved {Count} exception logs", errorLogs.Count);
                }
                else
                {
                    _logger.LogWarning("Failed to save exception logs");
                }

                return Ok(errorLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while analyzing exceptions");
                return StatusCode(500, new { error = "Internal server error occurred while analyzing exceptions" });
            }
        }

        /// <summary>
        /// Assigns basic severity to logs and generates AI-powered potential fixes when full AI analysis is not available
        /// </summary>
        private async Task AssignBasicSeverityWithAiFixesAsync(List<ErrorLog> logs)
        {
            var criticalKeywords = new[] { "outofmemory", "stackoverflow", "accessviolation", "fatal", "critical" };
            var highKeywords = new[] { "nullreference", "argumentnull", "unauthorized", "forbidden", "timeout" };
            var mediumKeywords = new[] { "notfound", "invalid", "format", "cast", "parse" };

            foreach (var log in logs)
            {
                var message = log.Message.ToLowerInvariant();
                var stackTrace = (log.StackTrace ?? "").ToLowerInvariant();
                var combined = $"{message} {stackTrace}";

                if (criticalKeywords.Any(keyword => combined.Contains(keyword)))
                {
                    log.Severity = "Critical";
                    log.Priority = "High";
                    log.AiReasoning = "Assigned Critical severity based on error pattern analysis";
                }
                else if (highKeywords.Any(keyword => combined.Contains(keyword)))
                {
                    log.Severity = "High";
                    log.Priority = "Medium";
                    log.AiReasoning = "Assigned High severity based on error pattern analysis";
                }
                else if (mediumKeywords.Any(keyword => combined.Contains(keyword)))
                {
                    log.Severity = "Medium";
                    log.Priority = "Low";
                    log.AiReasoning = "Assigned Medium severity based on error pattern analysis";
                }
                else
                {
                    log.Severity = "Low";
                    log.Priority = "Low";
                    log.AiReasoning = "Assigned Low severity as default classification";
                }

                log.PotentialFix = await GenerateContextualPotentialFixAsync(log);
                log.AnalyzedAt = DateTime.UtcNow;
                log.IsAnalyzed = true;
            }
        }

        /// <summary>
        /// Generates a contextual potential fix using AI based on the specific error content
        /// </summary>
        private async Task<string> GenerateContextualPotentialFixAsync(ErrorLog log)
        {
            try
            {
                var prompt = $"Analyze this specific error and provide a concise, actionable potential fix:\n\n" +
                            $"Error Message: {log.Message}\n" +
                            $"Stack Trace: {log.StackTrace ?? "Not available"}\n" +
                            $"Source: {log.Source}\n" +
                            $"Severity: {log.Severity}\n\n" +
                            $"Provide a specific, actionable potential fix suggestion (1-2 sentences max) based on this exact error:";

                var request = new ErrorLogPrioritization.Api.Controllers.SendMessageRequest { Message = prompt };
                var aiResponse = await AzureAiFoundryClient.ProcessFormAssistanceRequest(request);
                
                if (!string.IsNullOrEmpty(aiResponse))
                {
                    var cleanResponse = aiResponse.Trim();
                    if (cleanResponse.Length > 200)
                    {
                        cleanResponse = cleanResponse.Substring(0, 197) + "...";
                    }
                    return cleanResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate AI potential fix for log {LogId}, using contextual fallback", log.Id);
            }

            return GenerateContextualFallbackFix(log);
        }

        /// <summary>
        /// Generates contextual fallback fixes based on error content when AI service is unavailable
        /// </summary>
        private string GenerateContextualFallbackFix(ErrorLog log)
        {
            var message = log.Message.ToLowerInvariant();
            var stackTrace = (log.StackTrace ?? "").ToLowerInvariant();
            var combined = $"{message} {stackTrace}";

            if (combined.Contains("nullreference") || combined.Contains("argumentnull"))
                return $"Check for null values in {ExtractMethodName(log.StackTrace)} and add null validation before accessing object properties.";
            
            if (combined.Contains("timeout") || combined.Contains("timeoutexception"))
                return $"Increase timeout settings for {ExtractServiceName(log.Source)} or optimize the operation to complete faster.";
            
            if (combined.Contains("unauthorized") || combined.Contains("forbidden"))
                return $"Verify authentication credentials and permissions for {ExtractServiceName(log.Source)} access.";
            
            if (combined.Contains("outofmemory"))
                return $"Review memory usage in {ExtractMethodName(log.StackTrace)} and implement proper disposal of resources.";
            
            if (combined.Contains("format") || combined.Contains("cast") || combined.Contains("parse"))
                return $"Validate data format and type conversion in {ExtractMethodName(log.StackTrace)} before processing.";
            
            if (combined.Contains("notfound") || combined.Contains("filenotfound"))
                return $"Verify that required files or resources exist and are accessible from {ExtractServiceName(log.Source)}.";

            return $"Review the implementation in {ExtractMethodName(log.StackTrace) ?? ExtractServiceName(log.Source)} and add appropriate error handling.";
        }

        private string ExtractMethodName(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(stackTrace, @"at\s+([^(]+)");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private string ExtractServiceName(string source)
        {
            if (string.IsNullOrEmpty(source)) return "the service";
            var parts = source.Split('.');
            return parts.Length > 1 ? parts[parts.Length - 2] : source;
        }

        /// <summary>
        /// </summary>
        /// <param name="id">The ID of the error log to mark as resolved</param>
        /// <param name="request">Resolution details including status, timestamp, and user info</param>
        /// <returns>Success status</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///         "resolutionStatus": "Resolved",
        ///         "resolvedAt": "2024-01-15T10:30:00Z",
        ///         "resolvedBy": "Current User"
        /// 
        /// </remarks>
        [HttpPatch("{id}/resolve")]
        [SwaggerOperation(
            Summary = "Mark error log as resolved",
            Description = "Updates the resolution status of a specific error log to 'Resolved' with timestamp and user information.",
            OperationId = "MarkLogAsResolved",
            Tags = new[] { "Log Management" }
        )]
        [SwaggerResponse(200, "Log marked as resolved successfully", typeof(object))]
        [SwaggerResponse(400, "Invalid request data", typeof(object))]
        [SwaggerResponse(404, "Log not found", typeof(object))]
        [SwaggerResponse(500, "Internal server error", typeof(object))]
        public async Task<IActionResult> MarkLogAsResolved([FromRoute] string id, [FromBody] LogResolutionRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Received empty log ID for resolution");
                    return BadRequest(new { error = "Log ID cannot be empty" });
                }

                if (request == null)
                {
                    _logger.LogWarning("Received null resolution request for log {LogId}", id);
                    return BadRequest(new { error = "Resolution request cannot be null" });
                }

                // Validate the resolution status
                if (string.IsNullOrEmpty(request.ResolutionStatus) || request.ResolutionStatus != "Resolved")
                {
                    return BadRequest(new { error = "Resolution status must be 'Resolved'" });
                }

                var success = await _logService.UpdateLogResolutionStatusAsync(id, request.ResolutionStatus, request.ResolvedAt, request.ResolvedBy);
                
                if (success)
                {
                    _logger.LogInformation("Successfully marked log {LogId} as resolved by {ResolvedBy}", id, request.ResolvedBy);
                    return Ok(new { 
                        message = "Log marked as resolved successfully", 
                        logId = id,
                        resolutionStatus = request.ResolutionStatus,
                        resolvedAt = request.ResolvedAt,
                        resolvedBy = request.ResolvedBy
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to mark log {LogId} as resolved - log not found", id);
                    return NotFound(new { error = $"Log with ID {id} not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while marking log {LogId} as resolved", id);
                return StatusCode(500, new { error = "Internal server error occurred while updating log resolution status" });
            }
        }
    }
}
