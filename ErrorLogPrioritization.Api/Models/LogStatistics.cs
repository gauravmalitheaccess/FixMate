using System.Text.Json.Serialization;
using CommandLine.Text;
using NHibernate.Criterion;
using Swashbuckle.AspNetCore.Annotations;

namespace ErrorLogPrioritization.Api.Models
{
    /// <summary>
    /// Statistics about error logs for dashboard display
    /// </summary>
    [SwaggerSchema(Description = "Comprehensive statistics about error logs including counts by severity, priority, and analysis status")]
    public class LogStatistics
    {
        /// <summary>
        /// Total number of error logs
        /// </summary>
        [JsonPropertyName("totalLogs")]
        public int TotalLogs { get; set; }

        /// <summary>
        /// Number of critical severity logs
        /// </summary>
        [JsonPropertyName("criticalCount")]
        public int CriticalCount { get; set; }

        /// <summary>
        /// Number of high severity logs
        /// </summary>
        [JsonPropertyName("highCount")]
        public int HighCount { get; set; }

        /// <summary>
        /// Number of medium severity logs
        /// </summary>
        [JsonPropertyName("mediumCount")]
        public int MediumCount { get; set; }

        /// <summary>
        /// Number of low severity logs
        /// </summary>
        [JsonPropertyName("lowCount")]
        public int LowCount { get; set; }

        /// <summary>
        /// Number of logs that have been analyzed by AI
        /// </summary>
        [JsonPropertyName("analyzedCount")]
        public int AnalyzedCount { get; set; }

        /// <summary>
        /// Number of logs that have not been analyzed yet
        /// </summary>
        [JsonPropertyName("unanalyzedCount")]
        public int UnanalyzedCount { get; set; }

        /// <summary>
        /// Number of high priority logs
        /// </summary>
        [JsonPropertyName("highPriorityCount")]
        public int HighPriorityCount { get; set; }

        /// <summary>
        /// Number of medium priority logs
        /// </summary>
        [JsonPropertyName("mediumPriorityCount")]
        public int MediumPriorityCount { get; set; }

        /// <summary>
        /// Number of low priority logs
        /// </summary>
        [JsonPropertyName("lowPriorityCount")]
        public int LowPriorityCount { get; set; }

        /// <summary>
        /// Number of logs from today
        /// </summary>
        [JsonPropertyName("todayCount")]
        public int TodayCount { get; set; }

        /// <summary>
        /// Number of logs from the past week
        /// </summary>
        [JsonPropertyName("weekCount")]
        public int WeekCount { get; set; }

        /// <summary>
        /// Number of logs from the past month
        /// </summary>
        [JsonPropertyName("monthCount")]
        public int MonthCount { get; set; }

        /// <summary>
        /// Timestamp when these statistics were generated
        /// </summary>
        [JsonPropertyName("generatedAt")]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Number of logs that have been analyzed by AI
        /// </summary>
        [JsonPropertyName("analyzedLogs")]
        public int AnalyzedLogs { get; set; }

        /// <summary>
        /// Number of logs that have not been analyzed yet
        /// </summary>
        [JsonPropertyName("unanalyzedLogs")]
        public int UnanalyzedLogs { get; set; }

        /// <summary>
        /// Breakdown of logs by severity level
        /// </summary>
        [JsonPropertyName("severityBreakdown")]
        public Dictionary<string, int> SeverityBreakdown { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Breakdown of logs by priority level
        /// </summary>
        [JsonPropertyName("priorityBreakdown")]
        public Dictionary<string, int> PriorityBreakdown { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Date range for the statistics
        /// </summary>
        [JsonPropertyName("dateRange")]
        public DateRange DateRange { get; set; } = new DateRange();

        /// <summary>
        /// When the statistics were last updated
        /// </summary>
        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a date range for filtering statistics
    /// </summary>
    public class DateRange
    {
        /// <summary>
        /// Start date of the range
        /// </summary>
        [JsonPropertyName("fromDate")]
        public DateTime FromDate { get; set; }

        /// <summary>
        /// End date of the range
        /// </summary>
        [JsonPropertyName("toDate")]
        public DateTime ToDate { get; set; }
    }
}