using System.Text.Json.Serialization;

namespace ErrorLogPrioritization.Api.Models
{
    public class ExcelReportModel
    {
        [JsonPropertyName("reportDate")]
        public DateTime ReportDate { get; set; }

        [JsonPropertyName("logs")]
        public List<ExcelLogEntry> Logs { get; set; } = new List<ExcelLogEntry>();

        [JsonPropertyName("summary")]
        public ReportSummary Summary { get; set; } = new ReportSummary();
    }

    public class ExcelLogEntry
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = string.Empty;

        [JsonPropertyName("aiReasoning")]
        public string AiReasoning { get; set; } = string.Empty;

        [JsonPropertyName("potentialFix")]
        public string PotentialFix { get; set; } = string.Empty;

        [JsonPropertyName("stackTrace")]
        public string StackTrace { get; set; } = string.Empty;
    }

    public class ReportSummary
    {
        [JsonPropertyName("totalLogs")]
        public int TotalLogs { get; set; }

        [JsonPropertyName("criticalCount")]
        public int CriticalCount { get; set; }

        [JsonPropertyName("highCount")]
        public int HighCount { get; set; }

        [JsonPropertyName("mediumCount")]
        public int MediumCount { get; set; }

        [JsonPropertyName("lowCount")]
        public int LowCount { get; set; }

        [JsonPropertyName("analyzedCount")]
        public int AnalyzedCount { get; set; }

        [JsonPropertyName("unanalyzedCount")]
        public int UnanalyzedCount { get; set; }
    }
}
