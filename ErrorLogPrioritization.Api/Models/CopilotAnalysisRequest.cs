using System.Text.Json.Serialization;

namespace ErrorLogPrioritization.Api.Models
{
    public class CopilotAnalysisRequest
    {
        [JsonPropertyName("logs")]
        public List<LogEntry> Logs { get; set; } = new List<LogEntry>();

        [JsonPropertyName("context")]
        public HistoricalContext Context { get; set; } = new HistoricalContext();

        [JsonPropertyName("parameters")]
        public AnalysisParameters Parameters { get; set; } = new AnalysisParameters();
    }

    public class LogEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("stackTrace")]
        public string StackTrace { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;
    }

    public class HistoricalContext
    {
        [JsonPropertyName("previousAnalyses")]
        public List<string> PreviousAnalyses { get; set; } = new List<string>();

        [JsonPropertyName("frequentErrors")]
        public List<string> FrequentErrors { get; set; } = new List<string>();

        [JsonPropertyName("resolvedIssues")]
        public List<string> ResolvedIssues { get; set; } = new List<string>();

        [JsonPropertyName("previousAnalysisResults")]
        public List<ErrorLog> PreviousAnalysisResults { get; set; } = new List<ErrorLog>();

        [JsonPropertyName("errorPatterns")]
        public List<ErrorPattern> ErrorPatterns { get; set; } = new List<ErrorPattern>();

        [JsonPropertyName("analysisDate")]
        public DateTime AnalysisDate { get; set; }
    }

    public class ErrorPattern
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = string.Empty;

        [JsonPropertyName("frequency")]
        public int Frequency { get; set; }

        [JsonPropertyName("lastOccurrence")]
        public DateTime LastOccurrence { get; set; }
    }

    public class AnalysisParameters
    {
        [JsonPropertyName("includeSeverityClassification")]
        public bool IncludeSeverityClassification { get; set; } = true;

        [JsonPropertyName("includePriorityAssignment")]
        public bool IncludePriorityAssignment { get; set; } = true;

        [JsonPropertyName("includeReasoningExplanation")]
        public bool IncludeReasoningExplanation { get; set; } = true;

        [JsonPropertyName("maxResponseTime")]
        public int MaxResponseTimeSeconds { get; set; } = 30;
    }
}