using System.ComponentModel.DataAnnotations;

namespace ErrorLogPrioritization.Api.Configuration;

public class ConfigurationValidator
{
    public static void ValidateConfiguration(IConfiguration configuration, ILogger logger)
    {
        var errors = new List<string>();

        // Validate CopilotStudio configuration
        var copilotConfig = new CopilotStudioConfiguration();
        configuration.GetSection(CopilotStudioConfiguration.SectionName).Bind(copilotConfig);
        ValidateObject(copilotConfig, CopilotStudioConfiguration.SectionName, errors);

        // Validate FileStorage configuration
        var fileStorageConfig = new FileStorageConfiguration();
        configuration.GetSection(FileStorageConfiguration.SectionName).Bind(fileStorageConfig);
        ValidateObject(fileStorageConfig, FileStorageConfiguration.SectionName, errors);

        // Validate Scheduling configuration
        var schedulingConfig = new SchedulingConfiguration();
        configuration.GetSection(SchedulingConfiguration.SectionName).Bind(schedulingConfig);
        ValidateObject(schedulingConfig, SchedulingConfiguration.SectionName, errors);

        // Validate Performance configuration
        var performanceConfig = new PerformanceConfiguration();
        configuration.GetSection(PerformanceConfiguration.SectionName).Bind(performanceConfig);
        ValidateObject(performanceConfig, PerformanceConfiguration.SectionName, errors);

        // Custom validations
        ValidateCustomRules(configuration, errors);

        if (errors.Any())
        {
            var errorMessage = $"Configuration validation failed:\n{string.Join("\n", errors)}";
            logger.LogError("Configuration validation failed: {Errors}", string.Join(", ", errors));
            throw new InvalidOperationException(errorMessage);
        }

        logger.LogInformation("Configuration validation passed successfully");
    }

    private static void ValidateObject(object obj, string sectionName, List<string> errors)
    {
        var context = new ValidationContext(obj);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(obj, context, results, true))
        {
            foreach (var result in results)
            {
                errors.Add($"{sectionName}: {result.ErrorMessage}");
            }
        }
    }

    private static void ValidateCustomRules(IConfiguration configuration, List<string> errors)
    {
        // Validate that required directories can be created
        var fileStorageConfig = new FileStorageConfiguration();
        configuration.GetSection(FileStorageConfiguration.SectionName).Bind(fileStorageConfig);

        try
        {
            var logsPath = Path.GetFullPath(fileStorageConfig.LogsPath);
            var exportsPath = Path.GetFullPath(fileStorageConfig.ExportsPath);

            if (!Directory.Exists(logsPath) && fileStorageConfig.CreateDirectoriesIfNotExist)
            {
                Directory.CreateDirectory(logsPath);
            }

            if (!Directory.Exists(exportsPath) && fileStorageConfig.CreateDirectoriesIfNotExist)
            {
                Directory.CreateDirectory(exportsPath);
            }
        }
        catch (Exception ex)
        {
            errors.Add($"FileStorage: Unable to create or access storage directories: {ex.Message}");
        }

        // Validate timezone
        var schedulingConfig = new SchedulingConfiguration();
        configuration.GetSection(SchedulingConfiguration.SectionName).Bind(schedulingConfig);

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(schedulingConfig.TimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            errors.Add($"Scheduling: Invalid timezone '{schedulingConfig.TimeZone}'");
        }

        // Validate Copilot Studio URL accessibility (optional check)
        var copilotConfig = new CopilotStudioConfiguration();
        configuration.GetSection(CopilotStudioConfiguration.SectionName).Bind(copilotConfig);

        if (string.IsNullOrEmpty(copilotConfig.ApiKey) || copilotConfig.ApiKey == "your-api-key-here")
        {
            errors.Add("CopilotStudio: ApiKey must be configured with a valid API key");
        }

        if (copilotConfig.BaseUrl.Contains("your-copilot-studio-endpoint.com"))
        {
            errors.Add("CopilotStudio: BaseUrl must be configured with a valid endpoint URL");
        }

        if (copilotConfig.ApiUrl.Contains("your-copilot-studio-endpoint.com"))
        {
            errors.Add("CopilotStudio: ApiUrl must be configured with a valid endpoint URL");
        }
    }
}