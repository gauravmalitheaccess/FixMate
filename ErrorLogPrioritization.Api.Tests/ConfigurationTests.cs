using ErrorLogPrioritization.Api.Configuration;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace ErrorLogPrioritization.Api.Tests;

public class ConfigurationTests
{
    [Fact]
    public void CopilotStudioConfiguration_WithValidValues_ShouldPassValidation()
    {
        // Arrange
        var config = new CopilotStudioConfiguration
        {
            BaseUrl = "https://valid-endpoint.com",
            ApiUrl = "https://valid-endpoint.com/api/analyze",
            ApiKey = "valid-api-key",
            TimeoutSeconds = 30,
            MaxRetryAttempts = 3,
            RetryDelaySeconds = 5
        };

        // Act
        var validationResults = ValidateObject(config);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CopilotStudioConfiguration_WithInvalidUrl_ShouldFailValidation()
    {
        // Arrange
        var config = new CopilotStudioConfiguration
        {
            BaseUrl = "invalid-url",
            ApiUrl = "https://valid-endpoint.com/api/analyze",
            ApiKey = "valid-api-key"
        };

        // Act
        var validationResults = ValidateObject(config);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(CopilotStudioConfiguration.BaseUrl)));
    }

    [Fact]
    public void CopilotStudioConfiguration_WithEmptyApiKey_ShouldFailValidation()
    {
        // Arrange
        var config = new CopilotStudioConfiguration
        {
            BaseUrl = "https://valid-endpoint.com",
            ApiUrl = "https://valid-endpoint.com/api/analyze",
            ApiKey = ""
        };

        // Act
        var validationResults = ValidateObject(config);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(CopilotStudioConfiguration.ApiKey)));
    }

    [Fact]
    public void FileStorageConfiguration_WithValidValues_ShouldPassValidation()
    {
        // Arrange
        var config = new FileStorageConfiguration
        {
            LogsPath = "Data/Logs",
            ExportsPath = "Data/Exports",
            RetentionDays = 30,
            MaxFileSizeMB = 100
        };

        // Act
        var validationResults = ValidateObject(config);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void FileStorageConfiguration_WithEmptyPath_ShouldFailValidation()
    {
        // Arrange
        var config = new FileStorageConfiguration
        {
            LogsPath = "",
            ExportsPath = "Data/Exports"
        };

        // Act
        var validationResults = ValidateObject(config);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(FileStorageConfiguration.LogsPath)));
    }

    [Fact]
    public void SchedulingConfiguration_WithValidValues_ShouldPassValidation()
    {
        // Arrange
        var config = new SchedulingConfiguration
        {
            DailyAnalysisTime = "01:00:00",
            RetryIntervalMinutes = 30,
            MaxRetryAttempts = 3,
            TimeZone = "UTC"
        };

        // Act
        var validationResults = ValidateObject(config);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void SchedulingConfiguration_WithInvalidTimeFormat_ShouldFailValidation()
    {
        // Arrange
        var config = new SchedulingConfiguration
        {
            DailyAnalysisTime = "25:00:00",
            TimeZone = "UTC"
        };

        // Act
        var validationResults = ValidateObject(config);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(SchedulingConfiguration.DailyAnalysisTime)));
    }

    [Fact]
    public void PerformanceConfiguration_WithValidValues_ShouldPassValidation()
    {
        // Arrange
        var config = new PerformanceConfiguration
        {
            MaxLogsPerRequest = 10000,
            CacheExpirationSeconds = 300,
            MaxConcurrentAnalysis = 5,
            BatchSizeForProcessing = 100
        };

        // Act
        var validationResults = ValidateObject(config);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void PerformanceConfiguration_WithInvalidValues_ShouldFailValidation()
    {
        // Arrange
        var config = new PerformanceConfiguration
        {
            MaxLogsPerRequest = 0,
            CacheExpirationSeconds = 0,
            MaxConcurrentAnalysis = 0,
            BatchSizeForProcessing = 0
        };

        // Act
        var validationResults = ValidateObject(config);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(PerformanceConfiguration.MaxLogsPerRequest)));
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(PerformanceConfiguration.CacheExpirationSeconds)));
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(PerformanceConfiguration.MaxConcurrentAnalysis)));
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(PerformanceConfiguration.BatchSizeForProcessing)));
    }

    [Fact]
    public void ConfigurationBinding_ShouldWorkCorrectly()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["CopilotStudio:BaseUrl"] = "https://test-endpoint.com",
            ["CopilotStudio:ApiKey"] = "test-api-key",
            ["CopilotStudio:TimeoutSeconds"] = "45"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var copilotConfig = new CopilotStudioConfiguration();
        configuration.GetSection(CopilotStudioConfiguration.SectionName).Bind(copilotConfig);

        // Assert
        Assert.Equal("https://test-endpoint.com", copilotConfig.BaseUrl);
        Assert.Equal("test-api-key", copilotConfig.ApiKey);
        Assert.Equal(45, copilotConfig.TimeoutSeconds);
    }

    private static List<ValidationResult> ValidateObject(object obj)
    {
        var context = new ValidationContext(obj);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(obj, context, results, true);
        return results;
    }
}