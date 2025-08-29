using System.ComponentModel.DataAnnotations;

namespace ErrorLogPrioritization.Api.Configuration;

public class FileStorageConfiguration
{
    public const string SectionName = "FileStorage";

    [Required]
    [MinLength(1)]
    public string LogsPath { get; set; } = "Data/Logs";

    [Required]
    [MinLength(1)]
    public string ExportsPath { get; set; } = "Data/Exports";

    [Range(1, 365)]
    public int RetentionDays { get; set; } = 30;

    [Range(1, 10000)]
    public int MaxFileSizeMB { get; set; } = 100;

    public bool CreateDirectoriesIfNotExist { get; set; } = true;
}