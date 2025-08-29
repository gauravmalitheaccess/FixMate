using ErrorLogPrioritization.Api.Models;
using System.Text;
using System.Text.Json;
using Polly;
using Polly.Extensions.Http;

namespace ErrorLogPrioritization.Api.Services
{
    public interface ICopilotStudioService
    {
        Task<CopilotAnalysisResponse?> AnalyzeLogsAsync(List<ErrorLog> logs, CancellationToken cancellationToken = default);
        Task<CopilotAnalysisResponse?> AnalyzeLogsAsync(List<ErrorLog> logs, HistoricalContext? historicalContext, CancellationToken cancellationToken = default);
        Task<CopilotAnalysisRequest> BuildAnalysisRequestAsync(List<ErrorLog> logs, HistoricalContext? context = null);
        Task<List<ErrorLog>> ProcessAnalysisResultsAsync(List<ErrorLog> originalLogs, CopilotAnalysisResponse analysisResponse);
        Task UpdateLogsWithAnalysisAsync(List<ErrorLog> analyzedLogs, string filePath);
    }

    public class CopilotStudioService : ICopilotStudioService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CopilotStudioService> _logger;
        private readonly IConfiguration _configuration;
        private readonly Utils.IJsonFileManager _jsonFileManager;
        private readonly JsonSerializerOptions _jsonOptions;

        public CopilotStudioService(
            HttpClient httpClient, 
            ILogger<CopilotStudioService> logger,
            IConfiguration configuration,
            Utils.IJsonFileManager jsonFileManager)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _jsonFileManager = jsonFileManager;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            var baseUrl = _configuration["CopilotStudio:BaseUrl"];
            var apiKey = _configuration["CopilotStudio:ApiKey"];
            var timeout = _configuration.GetValue<int>("CopilotStudio:TimeoutSeconds", 30);

            if (!string.IsNullOrEmpty(baseUrl))
            {
                _httpClient.BaseAddress = new Uri(baseUrl);
            }

            _httpClient.Timeout = TimeSpan.FromSeconds(timeout);
            
            if (!string.IsNullOrEmpty(apiKey) && !_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            }

            // Content-Type will be set on individual requests
        }

        public async Task<CopilotAnalysisResponse?> AnalyzeLogsAsync(List<ErrorLog> logs, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting Copilot Studio analysis for {LogCount} logs", logs.Count);

                // Build analysis request with basic context (historical context should be provided by caller if available)
                var request = await BuildAnalysisRequestAsync(logs);
                var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogDebug("Sending analysis request to Copilot Studio with {LogCount} logs and {ContextLogCount} historical logs", 
                    request.Logs.Count, request.Context?.PreviousAnalysisResults?.Count ?? 0);

                var retryPolicy = GetRetryPolicy();
                
                var response = await retryPolicy.ExecuteAsync(async () =>
                {
                    var httpResponse = await _httpClient.PostAsync("/api/analyze", content, cancellationToken);
                    
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogError("Copilot Studio API returned error: {StatusCode} - {Content}", 
                            httpResponse.StatusCode, errorContent);
                        throw new HttpRequestException($"Copilot Studio API error: {httpResponse.StatusCode}");
                    }

                    return httpResponse;
                });

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var analysisResponse = JsonSerializer.Deserialize<CopilotAnalysisResponse>(responseContent, _jsonOptions);

                _logger.LogInformation("Copilot Studio analysis completed successfully for {LogCount} logs", logs.Count);
                return analysisResponse;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Copilot Studio analysis timed out after {TimeoutSeconds} seconds", 
                    _httpClient.Timeout.TotalSeconds);
                throw new TimeoutException("Copilot Studio analysis request timed out", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during Copilot Studio analysis");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Copilot Studio analysis");
                throw;
            }
        }

        public async Task<CopilotAnalysisResponse?> AnalyzeLogsAsync(List<ErrorLog> logs, HistoricalContext? historicalContext, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting Copilot Studio analysis for {LogCount} logs with historical context", logs.Count);

                // Build analysis request with provided historical context
                var request = await BuildAnalysisRequestAsync(logs, historicalContext);
                var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogDebug("Sending analysis request to Copilot Studio with {LogCount} logs and {ContextLogCount} historical logs", 
                    request.Logs.Count, request.Context?.PreviousAnalysisResults?.Count ?? 0);

                var retryPolicy = GetRetryPolicy();
                
                var response = await retryPolicy.ExecuteAsync(async () =>
                {
                    var httpResponse = await _httpClient.PostAsync("/api/analyze", content, cancellationToken);
                    
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogError("Copilot Studio API returned error: {StatusCode} - {Content}", 
                            httpResponse.StatusCode, errorContent);
                        throw new HttpRequestException($"Copilot Studio API error: {httpResponse.StatusCode}");
                    }

                    return httpResponse;
                });

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var analysisResponse = JsonSerializer.Deserialize<CopilotAnalysisResponse>(responseContent, _jsonOptions);

                _logger.LogInformation("Copilot Studio analysis with historical context completed successfully for {LogCount} logs", logs.Count);
                return analysisResponse;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Copilot Studio analysis with historical context timed out after {TimeoutSeconds} seconds", 
                    _httpClient.Timeout.TotalSeconds);
                throw new TimeoutException("Copilot Studio analysis request timed out", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during Copilot Studio analysis with historical context");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Copilot Studio analysis with historical context");
                throw;
            }
        }

        public async Task<CopilotAnalysisRequest> BuildAnalysisRequestAsync(List<ErrorLog> logs, HistoricalContext? context = null)
        {
            var logEntries = logs.Select(log => new LogEntry
            {
                Id = log.Id,
                Timestamp = log.Timestamp,
                Message = log.Message,
                StackTrace = log.StackTrace ?? string.Empty,
                Source = log.Source
            }).ToList();

            // Use provided context or build a basic one
            var historicalContext = context ?? await BuildBasicHistoricalContextAsync(logs);

            var analysisParameters = new AnalysisParameters
            {
                IncludeSeverityClassification = true,
                IncludePriorityAssignment = true,
                IncludeReasoningExplanation = true,
                MaxResponseTimeSeconds = _configuration.GetValue<int>("CopilotStudio:TimeoutSeconds", 30)
            };

            _logger.LogDebug("Built analysis request for {LogCount} logs with {ContextLogCount} historical logs", 
                logEntries.Count, historicalContext?.PreviousAnalysisResults?.Count ?? 0);

            return new CopilotAnalysisRequest
            {
                Logs = logEntries,
                Context = historicalContext,
                Parameters = analysisParameters
            };
        }

        private async Task<HistoricalContext> BuildBasicHistoricalContextAsync(List<ErrorLog> currentLogs)
        {
            // This method builds historical context from previous analyses
            // For now, we'll return a basic context, but this can be enhanced
            // to include actual historical data from previous analysis files or database
            
            var context = new HistoricalContext();

            // Group similar error messages to identify frequent errors
            var frequentErrors = currentLogs
                .GroupBy(log => GetErrorSignature(log.Message))
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .Take(10)
                .ToList();

            context.FrequentErrors = frequentErrors;

            // Add placeholder for previous analyses and resolved issues
            // These would typically come from a database or previous analysis files
            context.PreviousAnalyses = new List<string>
            {
                "Previous analysis patterns and trends would be included here"
            };

            context.ResolvedIssues = new List<string>
            {
                "Previously resolved issue patterns would be included here"
            };

            // Simulate async work (in real implementation, this would be database/file access)
            await Task.CompletedTask;
            
            return context;
        }

        private string GetErrorSignature(string errorMessage)
        {
            // Create a signature for the error by removing variable parts
            // This helps identify similar errors for frequency analysis
            if (string.IsNullOrEmpty(errorMessage))
                return string.Empty;

            // Remove common variable patterns like IDs, timestamps, etc.
            var signature = errorMessage;
            
            // Remove GUIDs
            signature = System.Text.RegularExpressions.Regex.Replace(signature, 
                @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b", 
                "[GUID]");
            
            // Remove numbers that might be IDs or timestamps
            signature = System.Text.RegularExpressions.Regex.Replace(signature, @"\b\d+\b", "[NUMBER]");
            
            // Remove file paths
            signature = System.Text.RegularExpressions.Regex.Replace(signature, 
                @"[A-Za-z]:\\[^\\]+(?:\\[^\\]+)*", "[FILEPATH]");

            return signature.Trim();
        }

        public Task<List<ErrorLog>> ProcessAnalysisResultsAsync(List<ErrorLog> originalLogs, CopilotAnalysisResponse analysisResponse)
        {
            try
            {
                _logger.LogInformation("Processing Copilot Studio analysis results for {LogCount} logs", originalLogs.Count);

                if (analysisResponse?.AnalyzedLogs == null || !analysisResponse.AnalyzedLogs.Any())
                {
                    _logger.LogWarning("No analysis results received from Copilot Studio");
                    return Task.FromResult(originalLogs);
                }

                var analyzedLogs = new List<ErrorLog>();
                var analysisLookup = analysisResponse.AnalyzedLogs.ToDictionary(a => a.LogId, a => a);

                foreach (var originalLog in originalLogs)
                {
                    var updatedLog = new ErrorLog
                    {
                        Id = originalLog.Id,
                        Timestamp = originalLog.Timestamp,
                        Source = originalLog.Source,
                        Message = originalLog.Message,
                        StackTrace = originalLog.StackTrace,
                        Severity = originalLog.Severity,
                        Priority = originalLog.Priority,
                        AiReasoning = originalLog.AiReasoning,
                        PotentialFix = originalLog.PotentialFix,
                        AnalyzedAt = originalLog.AnalyzedAt,
                        IsAnalyzed = originalLog.IsAnalyzed
                    };

                    if (analysisLookup.TryGetValue(originalLog.Id, out var analysisResult))
                    {
                        // Validate analysis result before applying
                        if (IsValidAnalysisResult(analysisResult))
                        {
                            updatedLog.Severity = analysisResult.Severity;
                            updatedLog.Priority = analysisResult.Priority;
                            updatedLog.AiReasoning = analysisResult.Reasoning;
                            updatedLog.PotentialFix = analysisResult.PotentialFix ?? string.Empty;
                            // updatedLog.AdoBug = analysisResult.AdoBug;
                            updatedLog.AnalyzedAt = DateTime.UtcNow;
                            updatedLog.IsAnalyzed = true;

                            _logger.LogDebug("Updated log {LogId} with AI analysis: Severity={Severity}, Priority={Priority}", 
                                originalLog.Id, analysisResult.Severity, analysisResult.Priority);
                        }
                        else
                        {
                            _logger.LogWarning("Invalid analysis result for log {LogId}, skipping AI update", originalLog.Id);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No analysis result found for log {LogId}", originalLog.Id);
                    }

                    analyzedLogs.Add(updatedLog);
                }

                _logger.LogInformation("Successfully processed analysis results for {ProcessedCount} logs", analyzedLogs.Count);
                return Task.FromResult(analyzedLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Copilot Studio analysis results");
                throw;
            }
        }

        public async Task UpdateLogsWithAnalysisAsync(List<ErrorLog> analyzedLogs, string filePath)
        {
            try
            {
                _logger.LogInformation("Updating logs file {FilePath} with analysis results for {LogCount} logs", 
                    filePath, analyzedLogs.Count);

                // Load existing logs from file
                var existingLogs = await _jsonFileManager.LoadLogsAsync(filePath);
                var existingLogsLookup = existingLogs.ToDictionary(log => log.Id, log => log);

                // Update existing logs with analysis results
                var updatedLogs = new List<ErrorLog>();
                foreach (var analyzedLog in analyzedLogs)
                {
                    if (existingLogsLookup.TryGetValue(analyzedLog.Id, out var existingLog))
                    {
                        // Update the existing log with analysis results
                        existingLog.Severity = analyzedLog.Severity;
                        existingLog.Priority = analyzedLog.Priority;
                        existingLog.AiReasoning = analyzedLog.AiReasoning;
                        existingLog.PotentialFix = analyzedLog.PotentialFix;
                        // existingLog.AdoBug = analyzedLog.AdoBug;
                        existingLog.AnalyzedAt = analyzedLog.AnalyzedAt;
                        existingLog.IsAnalyzed = analyzedLog.IsAnalyzed;
                        updatedLogs.Add(existingLog);
                    }
                    else
                    {
                        // Add new log if it doesn't exist
                        updatedLogs.Add(analyzedLog);
                    }
                }

                // Add any remaining existing logs that weren't in the analyzed set
                foreach (var existingLog in existingLogs)
                {
                    if (!analyzedLogs.Any(a => a.Id == existingLog.Id))
                    {
                        updatedLogs.Add(existingLog);
                    }
                }

                // Sort by timestamp to maintain chronological order
                updatedLogs = updatedLogs.OrderBy(log => log.Timestamp).ToList();

                // Save updated logs back to file
                await _jsonFileManager.SaveLogsAsync(filePath, updatedLogs);

                _logger.LogInformation("Successfully updated logs file {FilePath} with analysis results", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating logs file {FilePath} with analysis results", filePath);
                throw;
            }
        }

        private bool IsValidAnalysisResult(AnalyzedLog analysisResult)
        {
            if (analysisResult == null)
                return false;

            // Validate severity
            var validSeverities = new[] { "Critical", "High", "Medium", "Low" };
            if (string.IsNullOrEmpty(analysisResult.Severity) || !validSeverities.Contains(analysisResult.Severity))
            {
                _logger.LogWarning("Invalid severity value: {Severity}", analysisResult.Severity);
                return false;
            }

            // Validate priority
            var validPriorities = new[] { "High", "Medium", "Low" };
            if (string.IsNullOrEmpty(analysisResult.Priority) || !validPriorities.Contains(analysisResult.Priority))
            {
                _logger.LogWarning("Invalid priority value: {Priority}", analysisResult.Priority);
                return false;
            }

            // Validate confidence score
            if (analysisResult.ConfidenceScore < 0 || analysisResult.ConfidenceScore > 1)
            {
                _logger.LogWarning("Invalid confidence score: {ConfidenceScore}", analysisResult.ConfidenceScore);
                return false;
            }

            // Require reasoning to be provided
            if (string.IsNullOrWhiteSpace(analysisResult.Reasoning))
            {
                _logger.LogWarning("Missing reasoning for analysis result");
                return false;
            }

            return true;
        }

        private IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => !msg.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning("Copilot Studio API retry attempt {RetryCount} after {Delay}ms", 
                            retryCount, timespan.TotalMilliseconds);
                    });
        }
    }
}
