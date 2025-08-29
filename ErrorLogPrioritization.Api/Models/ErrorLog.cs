using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace ErrorLogPrioritization.Api.Models
{
    /// <summary>
    /// Represents an error log entry from a web application
    /// </summary>
    [SwaggerSchema(Description = "Error log entry containing details about application errors and their AI-powered analysis")]
    public class ErrorLogModel
    {
        /// <summary>
        /// </summary>
        [JsonPropertyName("errorLog")]
        public ErrorLog ErrorLog { get; set; }

        /// <summary>
        /// </summary>
        [JsonPropertyName("adoBug")]
        public AdoBug? AdoBug { get; set; }
    }

    public class ErrorLog
    {
        /// <summary>
        /// Unique identifier for the error log entry
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp when the error occurred
        /// </summary>
        [JsonPropertyName("timestamp")]
        [Required]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Source location where the error occurred (e.g., class name, method)
        /// </summary>
        [JsonPropertyName("source")]
        [Required]
        [StringLength(500)]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Error message describing what went wrong
        /// </summary>
        [JsonPropertyName("message")]
        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Stack trace providing detailed error context
        /// </summary>
        [JsonPropertyName("stackTrace")]
        public string StackTrace { get; set; } = string.Empty;

        /// <summary>
        /// Severity level of the error (Critical, High, Medium, Low)
        /// </summary>
        [JsonPropertyName("severity")]
        [StringLength(50)]
        public string Severity { get; set; } = string.Empty;

        /// <summary>
        /// AI-determined priority level (High, Medium, Low)
        /// </summary>
        [JsonPropertyName("priority")]
        [StringLength(50)]
        public string Priority { get; set; } = string.Empty;

        /// <summary>
        /// AI-generated reasoning for the priority assignment
        /// </summary>
        [JsonPropertyName("aiReasoning")]
        public string AiReasoning { get; set; } = string.Empty;

        /// <summary>
        /// AI-generated potential fix suggestion for the error
        /// </summary>
        [JsonPropertyName("potentialFix")]
        [StringLength(2000)]
        public string PotentialFix { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the AI analysis was completed
        /// </summary>
        [JsonPropertyName("analyzedAt")]
        public DateTime? AnalyzedAt { get; set; }

        /// <summary>
        /// Indicates whether the log has been analyzed by AI
        /// </summary>
        [JsonPropertyName("isAnalyzed")]
        public bool IsAnalyzed { get; set; } = false;

        /// <summary>
        /// </summary>
        [JsonPropertyName("resolutionStatus")]
        [StringLength(50)]
        public string ResolutionStatus { get; set; } = "Pending";

        /// <summary>
        /// Timestamp when the log was marked as resolved
        /// </summary>
        [JsonPropertyName("resolvedAt")]
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// </summary>
        [JsonPropertyName("resolvedBy")]
        [StringLength(100)]
        public string? ResolvedBy { get; set; }

        /// <summary>
        /// </summary>
        [JsonPropertyName("adoBug")]
        public AdoBug? AdoBug { get; set; }
    }
}
