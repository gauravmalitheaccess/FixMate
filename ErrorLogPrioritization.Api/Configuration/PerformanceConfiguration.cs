using System.ComponentModel.DataAnnotations;

namespace ErrorLogPrioritization.Api.Configuration;

public class PerformanceConfiguration
{
    public const string SectionName = "Performance";

    [Range(100, 100000)]
    public int MaxLogsPerRequest { get; set; } = 10000;

    [Range(1, 3600)]
    public int CacheExpirationSeconds { get; set; } = 300;

    [Range(1, 100)]
    public int MaxConcurrentAnalysis { get; set; } = 5;

    [Range(1, 1000)]
    public int BatchSizeForProcessing { get; set; } = 100;

    public bool EnablePerformanceMonitoring { get; set; } = true;
}