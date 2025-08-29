using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace ErrorLogPrioritization.Api.Models
{
    /// <summary>
    /// </summary>
    [SwaggerSchema(Description = "ADO bug details for Azure DevOps work item creation")]
    public class AdoBug
    {
        /// <summary>
        /// </summary>
        [JsonPropertyName("title")]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// </summary>
        [JsonPropertyName("reproSteps")]
        public string ReproSteps { get; set; } = string.Empty;

        /// <summary>
        /// </summary>
        [JsonPropertyName("severity")]
        [StringLength(50)]
        public string Severity { get; set; } = string.Empty;

        /// <summary>
        /// </summary>
        [JsonPropertyName("priority")]
        [StringLength(50)]
        public string Priority { get; set; } = string.Empty;

        /// <summary>
        /// </summary>
        [JsonPropertyName("areaPath")]
        [StringLength(500)]
        public string AreaPath { get; set; } = string.Empty;

        /// <summary>
        /// </summary>
        [JsonPropertyName("assignedTo")]
        [StringLength(100)]
        public string AssignedTo { get; set; } = string.Empty;

        /// <summary>
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();
    }
}
