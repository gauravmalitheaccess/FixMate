using System.ComponentModel.DataAnnotations;

namespace ErrorLogPrioritization.Api.Models
{
    /// <summary>
    /// Represents raw exception data received from applications before AI analysis
    /// </summary>
    public class RawExceptionData
    {
        /// <summary>
        /// Source of the exception (e.g., class.method, controller.action)
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Exception message
        /// </summary>
        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Full stack trace of the exception
        /// </summary>
        [StringLength(10000)]
        public string? StackTrace { get; set; }

        /// <summary>
        /// Optional timestamp when the exception occurred (if not provided, current UTC time will be used)
        /// </summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Optional additional context or metadata about the exception
        /// </summary>
        [StringLength(1000)]
        public string? Context { get; set; }

        /// <summary>
        /// Optional user ID or identifier associated with the exception
        /// </summary>
        [StringLength(100)]
        public string? UserId { get; set; }

        /// <summary>
        /// Optional session ID or request ID for tracking
        /// </summary>
        [StringLength(100)]
        public string? SessionId { get; set; }

        /// <summary>
        /// Optional application version or build number
        /// </summary>
        [StringLength(50)]
        public string? AppVersion { get; set; }

        /// <summary>
        /// Optional environment where the exception occurred (e.g., Production, Staging, Development)
        /// </summary>
        [StringLength(50)]
        public string? Environment { get; set; }
    }
}