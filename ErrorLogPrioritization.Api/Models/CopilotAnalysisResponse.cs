using System.Text.Json.Serialization;

namespace ErrorLogPrioritization.Api.Models
{
    public class CopilotAnalysisResponse
    {
        [JsonPropertyName("analyzedLogs")]
        public List<AnalyzedLog> AnalyzedLogs { get; set; } = new List<AnalyzedLog>();

        [JsonPropertyName("overallAssessment")]
        public string OverallAssessment { get; set; } = string.Empty;

        [JsonPropertyName("recommendations")]
        public List<string> Recommendations { get; set; } = new List<string>();

        [JsonPropertyName("analysisTimestamp")]
        public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
    }

    public class AnalyzedLog
    {
        [JsonPropertyName("logId")]
        public string LogId { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = string.Empty;

        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = string.Empty;

        [JsonPropertyName("potentialFix")]
        public string PotentialFix { get; set; } = string.Empty;

        [JsonPropertyName("confidenceScore")]
        public double ConfidenceScore { get; set; }

        [JsonPropertyName("adoBug")]
        public AdoBug? AdoBug { get; set; }
    }
}
