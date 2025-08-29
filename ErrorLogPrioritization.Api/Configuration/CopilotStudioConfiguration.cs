using System.ComponentModel.DataAnnotations;

namespace ErrorLogPrioritization.Api.Configuration;

public class CopilotStudioConfiguration
{
    public const string SectionName = "CopilotStudio";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    [Url]
    public string ApiUrl { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string ApiKey { get; set; } = string.Empty;

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    [Range(1, 60)]
    public int RetryDelaySeconds { get; set; } = 5;
}