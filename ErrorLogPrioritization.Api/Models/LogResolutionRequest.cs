using System.ComponentModel.DataAnnotations;

namespace ErrorLogPrioritization.Api.Models
{
    /// <summary>
    /// </summary>
    public class LogResolutionRequest
    {
        /// <summary>
        /// </summary>
        [Required]
        public string ResolutionStatus { get; set; } = string.Empty;

        /// <summary>
        /// </summary>
        [Required]
        public DateTime ResolvedAt { get; set; }

        /// <summary>
        /// </summary>
        [Required]
        public string ResolvedBy { get; set; } = string.Empty;
    }
}
