using System.ComponentModel.DataAnnotations;

namespace ErrorLogPrioritization.Api.Configuration;

public class SchedulingConfiguration
{
    public const string SectionName = "Scheduling";

    [Required]
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$", 
        ErrorMessage = "DailyAnalysisTime must be in HH:mm:ss format")]
    public string DailyAnalysisTime { get; set; } = "01:00:00";

    [Range(1, 1440)]
    public int RetryIntervalMinutes { get; set; } = 30;

    [Range(1, 24)]
    public int MaxRetryAttempts { get; set; } = 3;

    public bool EnableScheduledAnalysis { get; set; } = true;

    [Required]
    public string TimeZone { get; set; } = "UTC";
}