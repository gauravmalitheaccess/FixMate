using ErrorLogPrioritization.Api.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ErrorLogPrioritization.Api.Tests;

public class ConfigurationValidatorTests
{
    private readonly Mock<ILogger> _mockLogger;

    public ConfigurationValidatorTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public void ValidateConfiguration_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var configuration = CreateValidConfiguration();

        // Act & Assert
        var exception = Record.Exception(() => ConfigurationValidator.ValidateConfiguration(configuration, _mockLogger.Object));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateConfiguration_WithMissingCopilotStudioApiKey_ShouldThrow()
    {
        // Arrange
        var configData = GetValidConfigurationData();
        configData["CopilotStudio:ApiKey"] = "";
        var configuration = CreateConfiguration(configData);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            ConfigurationValidator.ValidateConfiguration(configuration, _mockLogger.Object));
        Assert.Contains("CopilotStudio", exception.Message);
        Assert.Contains("ApiKey", exception.Message);
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidUrl_ShouldThrow()
    {
        // Arrange
        var configData = GetValidConfigurationData();
        configData["CopilotStudio:BaseUrl"] = "invalid-url";
        var configuration = CreateConfiguration(configData);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            ConfigurationValidator.ValidateConfiguration(configuration, _mockLogger.Object));
        Assert.Contains("CopilotStudio", exception.Message);
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidTimeoutSeconds_ShouldThrow()
    {
        // Arrange
        var configData = GetValidConfigurationData();
        configData["CopilotStudio:TimeoutSeconds"] = "0";
        var configuration = CreateConfiguration(configData);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            ConfigurationValidator.ValidateConfiguration(configuration, _mockLogger.Object));
        Assert.Contains("CopilotStudio", exception.Message);
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidDailyAnalysisTime_ShouldThrow()
    {
        // Arrange
        var configData = GetValidConfigurationData();
        configData["Scheduling:DailyAnalysisTime"] = "25:00:00";
        var configuration = CreateConfiguration(configData);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            ConfigurationValidator.ValidateConfiguration(configuration, _mockLogger.Object));
        Assert.Contains("Scheduling", exception.Message);
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidTimeZone_ShouldThrow()
    {
        // Arrange
        var configData = GetValidConfigurationData();
        configData["Scheduling:TimeZone"] = "Invalid/TimeZone";
        var configuration = CreateConfiguration(configData);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            ConfigurationValidator.ValidateConfiguration(configuration, _mockLogger.Object));
        Assert.Contains("Invalid timezone", exception.Message);
    }

    [Fact]
    public void ValidateConfiguration_WithEmptyFileStoragePath_ShouldThrow()
    {
        // Arrange
        var configData = GetValidConfigurationData();
        configData["FileStorage:LogsPath"] = "";
        var configuration = CreateConfiguration(configData);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            ConfigurationValidator.ValidateConfiguration(configuration, _mockLogger.Object));
        Assert.Contains("FileStorage", exception.Message);
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidPerformanceValues_ShouldThrow()
    {
        // Arrange
        var configData = GetValidConfigurationData();
        configData["Performance:MaxLogsPerRequest"] = "0";
        var configuration = CreateConfiguration(configData);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            ConfigurationValidator.ValidateConfiguration(configuration, _mockLogger.Object));
        Assert.Contains("Performance", exception.Message);
    }

    [Fact]
    public void ValidateConfiguration_WithDefaultCopilotStudioUrls_ShouldThrow()
    {
        // Arrange
        var configData = GetValidConfigurationData();
        configData["CopilotStudio:BaseUrl"] = "https://your-copilot-studio-endpoint.com";
        configData["CopilotStudio:ApiKey"] = "valid-api-key";
        var configuration = CreateConfiguration(configData);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            ConfigurationValidator.ValidateConfiguration(configuration, _mockLogger.Object));
        Assert.Contains("BaseUrl must be configured", exception.Message);
    }

    private IConfiguration CreateValidConfiguration()
    {
        return CreateConfiguration(GetValidConfigurationData());
    }

    private Dictionary<string, string> GetValidConfigurationData()
    {
        return new Dictionary<string, string>
        {
            // CopilotStudio configuration
            ["CopilotStudio:BaseUrl"] = "https://valid-copilot-studio.com",
            ["CopilotStudio:ApiUrl"] = "https://valid-copilot-studio.com/api/analyze",
            ["CopilotStudio:ApiKey"] = "valid-api-key-123",
            ["CopilotStudio:TimeoutSeconds"] = "30",
            ["CopilotStudio:MaxRetryAttempts"] = "3",
            ["CopilotStudio:RetryDelaySeconds"] = "5",

            // FileStorage configuration
            ["FileStorage:LogsPath"] = "Data/Logs",
            ["FileStorage:ExportsPath"] = "Data/Exports",
            ["FileStorage:RetentionDays"] = "30",
            ["FileStorage:MaxFileSizeMB"] = "100",
            ["FileStorage:CreateDirectoriesIfNotExist"] = "true",

            // Scheduling configuration
            ["Scheduling:DailyAnalysisTime"] = "01:00:00",
            ["Scheduling:RetryIntervalMinutes"] = "30",
            ["Scheduling:MaxRetryAttempts"] = "3",
            ["Scheduling:EnableScheduledAnalysis"] = "true",
            ["Scheduling:TimeZone"] = "UTC",

            // Performance configuration
            ["Performance:MaxLogsPerRequest"] = "10000",
            ["Performance:CacheExpirationSeconds"] = "300",
            ["Performance:MaxConcurrentAnalysis"] = "5",
            ["Performance:BatchSizeForProcessing"] = "100",
            ["Performance:EnablePerformanceMonitoring"] = "true"
        };
    }

    private IConfiguration CreateConfiguration(Dictionary<string, string> configData)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }
}