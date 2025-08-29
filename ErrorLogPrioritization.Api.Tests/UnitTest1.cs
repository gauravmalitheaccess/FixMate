using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ErrorLogPrioritization.Api.Models;

namespace ErrorLogPrioritization.Api.Tests;

public class ErrorLogModelTests
{
    [Fact]
    public void ErrorLog_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var errorLog = new ErrorLog();

        // Assert
        Assert.NotNull(errorLog.Id);
        Assert.NotEmpty(errorLog.Id);
        Assert.Equal(string.Empty, errorLog.Source);
        Assert.Equal(string.Empty, errorLog.Message);
        Assert.Equal(string.Empty, errorLog.StackTrace);
        Assert.Equal(string.Empty, errorLog.Severity);
        Assert.Equal(string.Empty, errorLog.Priority);
        Assert.Equal(string.Empty, errorLog.AiReasoning);
        Assert.Null(errorLog.AnalyzedAt);
        Assert.False(errorLog.IsAnalyzed);
    }

    [Fact]
    public void ErrorLog_AllProperties_ShouldBeSettable()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var analyzedAt = DateTime.UtcNow.AddHours(1);
        var errorLog = new ErrorLog();

        // Act
        errorLog.Id = "test-id";
        errorLog.Timestamp = timestamp;
        errorLog.Source = "TestApp";
        errorLog.Message = "Test error message";
        errorLog.StackTrace = "Test stack trace";
        errorLog.Severity = "High";
        errorLog.Priority = "Critical";
        errorLog.AiReasoning = "Test reasoning";
        errorLog.AnalyzedAt = analyzedAt;
        errorLog.IsAnalyzed = true;

        // Assert
        Assert.Equal("test-id", errorLog.Id);
        Assert.Equal(timestamp, errorLog.Timestamp);
        Assert.Equal("TestApp", errorLog.Source);
        Assert.Equal("Test error message", errorLog.Message);
        Assert.Equal("Test stack trace", errorLog.StackTrace);
        Assert.Equal("High", errorLog.Severity);
        Assert.Equal("Critical", errorLog.Priority);
        Assert.Equal("Test reasoning", errorLog.AiReasoning);
        Assert.Equal(analyzedAt, errorLog.AnalyzedAt);
        Assert.True(errorLog.IsAnalyzed);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ErrorLog_RequiredFields_ShouldFailValidation_WhenEmpty(string? value)
    {
        // Arrange
        var errorLog = new ErrorLog
        {
            Timestamp = DateTime.UtcNow,
            Source = value!,
            Message = value!
        };

        // Act
        var validationResults = ValidateModel(errorLog);

        // Assert
        Assert.True(validationResults.Count > 0);
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Source"));
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Message"));
    }

    [Fact]
    public void ErrorLog_StringLengthValidation_ShouldFailForTooLongValues()
    {
        // Arrange
        var errorLog = new ErrorLog
        {
            Timestamp = DateTime.UtcNow,
            Source = new string('a', 501), // Exceeds 500 character limit
            Message = new string('b', 2001), // Exceeds 2000 character limit
            Severity = new string('c', 51), // Exceeds 50 character limit
            Priority = new string('d', 51) // Exceeds 50 character limit
        };

        // Act
        var validationResults = ValidateModel(errorLog);

        // Assert
        Assert.True(validationResults.Count >= 4);
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Source"));
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Message"));
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Severity"));
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Priority"));
    }

    [Fact]
    public void ErrorLog_JsonSerialization_ShouldSerializeCorrectly()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var analyzedAt = new DateTime(2024, 1, 15, 11, 30, 0, DateTimeKind.Utc);
        var errorLog = new ErrorLog
        {
            Id = "test-id-123",
            Timestamp = timestamp,
            Source = "TestApplication",
            Message = "Test error occurred",
            StackTrace = "at TestMethod() line 42",
            Severity = "High",
            Priority = "Critical",
            AiReasoning = "Critical due to user impact",
            AnalyzedAt = analyzedAt,
            IsAnalyzed = true
        };

        // Act
        var json = JsonSerializer.Serialize(errorLog);
        var deserializedLog = JsonSerializer.Deserialize<ErrorLog>(json);

        // Assert
        Assert.NotNull(deserializedLog);
        Assert.Equal(errorLog.Id, deserializedLog.Id);
        Assert.Equal(errorLog.Timestamp, deserializedLog.Timestamp);
        Assert.Equal(errorLog.Source, deserializedLog.Source);
        Assert.Equal(errorLog.Message, deserializedLog.Message);
        Assert.Equal(errorLog.StackTrace, deserializedLog.StackTrace);
        Assert.Equal(errorLog.Severity, deserializedLog.Severity);
        Assert.Equal(errorLog.Priority, deserializedLog.Priority);
        Assert.Equal(errorLog.AiReasoning, deserializedLog.AiReasoning);
        Assert.Equal(errorLog.AnalyzedAt, deserializedLog.AnalyzedAt);
        Assert.Equal(errorLog.IsAnalyzed, deserializedLog.IsAnalyzed);
    }

    [Fact]
    public void ErrorLog_JsonSerialization_ShouldUseCorrectPropertyNames()
    {
        // Arrange
        var errorLog = new ErrorLog
        {
            Id = "test-id",
            Timestamp = DateTime.UtcNow,
            Source = "TestApp",
            Message = "Test message",
            StackTrace = "Test stack",
            Severity = "High",
            Priority = "Critical",
            AiReasoning = "Test reasoning",
            AnalyzedAt = DateTime.UtcNow,
            IsAnalyzed = true
        };

        // Act
        var json = JsonSerializer.Serialize(errorLog);

        // Assert
        Assert.Contains("\"id\":", json);
        Assert.Contains("\"timestamp\":", json);
        Assert.Contains("\"source\":", json);
        Assert.Contains("\"message\":", json);
        Assert.Contains("\"stackTrace\":", json);
        Assert.Contains("\"severity\":", json);
        Assert.Contains("\"priority\":", json);
        Assert.Contains("\"aiReasoning\":", json);
        Assert.Contains("\"analyzedAt\":", json);
        Assert.Contains("\"isAnalyzed\":", json);
    }

    [Fact]
    public void ErrorLog_ValidModel_ShouldPassValidation()
    {
        // Arrange
        var errorLog = new ErrorLog
        {
            Timestamp = DateTime.UtcNow,
            Source = "TestApplication",
            Message = "Valid error message",
            StackTrace = "Valid stack trace",
            Severity = "Medium",
            Priority = "Low"
        };

        // Act
        var validationResults = ValidateModel(errorLog);

        // Assert
        Assert.Empty(validationResults);
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}
