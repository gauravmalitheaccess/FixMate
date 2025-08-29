using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ErrorLogPrioritization.Api.Models;
using ErrorLogPrioritization.Api.Utils;

namespace ErrorLogPrioritization.Api.Tests;

public class JsonFileManagerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<JsonFileManager>> _mockLogger;
    private readonly JsonFileManager _jsonFileManager;

    public JsonFileManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "JsonFileManagerTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["FileStorage:LogsPath"]).Returns(_testDirectory);

        _mockLogger = new Mock<ILogger<JsonFileManager>>();
        _jsonFileManager = new JsonFileManager(_mockConfiguration.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task LoadLogsAsync_FileDoesNotExist_ShouldReturnEmptyList()
    {
        // Arrange
        var filePath = "non-existent-file.json";

        // Act
        var result = await _jsonFileManager.LoadLogsAsync(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadLogsAsync_EmptyFile_ShouldReturnEmptyList()
    {
        // Arrange
        var filePath = "empty-file.json";
        var fullPath = Path.Combine(_testDirectory, filePath);
        await File.WriteAllTextAsync(fullPath, "");

        // Act
        var result = await _jsonFileManager.LoadLogsAsync(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveLogsAsync_ValidLogs_ShouldSaveSuccessfully()
    {
        // Arrange
        var filePath = "test-logs.json";
        var logs = new List<ErrorLog>
        {
            new ErrorLog
            {
                Id = "test-1",
                Timestamp = DateTime.UtcNow,
                Source = "TestApp",
                Message = "Test error 1",
                Severity = "High"
            },
            new ErrorLog
            {
                Id = "test-2",
                Timestamp = DateTime.UtcNow,
                Source = "TestApp",
                Message = "Test error 2",
                Severity = "Medium"
            }
        };

        // Act
        await _jsonFileManager.SaveLogsAsync(filePath, logs);

        // Assert
        var fullPath = Path.Combine(_testDirectory, filePath);
        Assert.True(File.Exists(fullPath));
        
        var savedContent = await File.ReadAllTextAsync(fullPath);
        Assert.NotEmpty(savedContent);
        
        var deserializedLogs = JsonSerializer.Deserialize<List<ErrorLog>>(savedContent);
        Assert.NotNull(deserializedLogs);
        Assert.Equal(2, deserializedLogs.Count);
        Assert.Equal("test-1", deserializedLogs[0].Id);
        Assert.Equal("test-2", deserializedLogs[1].Id);
    }

    [Fact]
    public async Task LoadLogsAsync_ValidFile_ShouldLoadCorrectly()
    {
        // Arrange
        var filePath = "load-test.json";
        var originalLogs = new List<ErrorLog>
        {
            new ErrorLog
            {
                Id = "load-test-1",
                Timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                Source = "LoadTestApp",
                Message = "Load test error",
                StackTrace = "Test stack trace",
                Severity = "Critical",
                Priority = "High",
                AiReasoning = "Test reasoning",
                AnalyzedAt = new DateTime(2024, 1, 15, 11, 30, 0, DateTimeKind.Utc),
                IsAnalyzed = true
            }
        };

        // Save logs first
        await _jsonFileManager.SaveLogsAsync(filePath, originalLogs);

        // Act
        var loadedLogs = await _jsonFileManager.LoadLogsAsync(filePath);

        // Assert
        Assert.NotNull(loadedLogs);
        Assert.Single(loadedLogs);
        
        var loadedLog = loadedLogs[0];
        Assert.Equal("load-test-1", loadedLog.Id);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc), loadedLog.Timestamp);
        Assert.Equal("LoadTestApp", loadedLog.Source);
        Assert.Equal("Load test error", loadedLog.Message);
        Assert.Equal("Test stack trace", loadedLog.StackTrace);
        Assert.Equal("Critical", loadedLog.Severity);
        Assert.Equal("High", loadedLog.Priority);
        Assert.Equal("Test reasoning", loadedLog.AiReasoning);
        Assert.Equal(new DateTime(2024, 1, 15, 11, 30, 0, DateTimeKind.Utc), loadedLog.AnalyzedAt);
        Assert.True(loadedLog.IsAnalyzed);
    }

    [Fact]
    public async Task AppendLogAsync_ExistingFile_ShouldAddToExistingLogs()
    {
        // Arrange
        var filePath = "append-test.json";
        var existingLogs = new List<ErrorLog>
        {
            new ErrorLog
            {
                Id = "existing-1",
                Timestamp = DateTime.UtcNow,
                Source = "ExistingApp",
                Message = "Existing error"
            }
        };
        
        await _jsonFileManager.SaveLogsAsync(filePath, existingLogs);

        var newLog = new ErrorLog
        {
            Id = "new-1",
            Timestamp = DateTime.UtcNow,
            Source = "NewApp",
            Message = "New error"
        };

        // Act
        await _jsonFileManager.AppendLogAsync(filePath, newLog);

        // Assert
        var allLogs = await _jsonFileManager.LoadLogsAsync(filePath);
        Assert.Equal(2, allLogs.Count);
        Assert.Contains(allLogs, log => log.Id == "existing-1");
        Assert.Contains(allLogs, log => log.Id == "new-1");
    }

    [Fact]
    public async Task AppendLogAsync_NewFile_ShouldCreateFileWithSingleLog()
    {
        // Arrange
        var filePath = "new-append-test.json";
        var newLog = new ErrorLog
        {
            Id = "single-log",
            Timestamp = DateTime.UtcNow,
            Source = "SingleApp",
            Message = "Single error"
        };

        // Act
        await _jsonFileManager.AppendLogAsync(filePath, newLog);

        // Assert
        var allLogs = await _jsonFileManager.LoadLogsAsync(filePath);
        Assert.Single(allLogs);
        Assert.Equal("single-log", allLogs[0].Id);
    }

    [Theory]
    [InlineData("2024-01-15", "logs-2024-01-15.json")]
    [InlineData("2024-12-31", "logs-2024-12-31.json")]
    [InlineData("2023-02-28", "logs-2023-02-28.json")]
    public void GetDailyLogFilePath_ValidDates_ShouldReturnCorrectFormat(string dateString, string expectedFileName)
    {
        // Arrange
        var date = DateTime.Parse(dateString);

        // Act
        var result = _jsonFileManager.GetDailyLogFilePath(date);

        // Assert
        Assert.Equal(expectedFileName, result);
    }

    [Fact]
    public async Task LoadLogsByDateRangeAsync_MultipleDays_ShouldLoadFromMultipleFiles()
    {
        // Arrange
        var date1 = new DateTime(2024, 1, 15);
        var date2 = new DateTime(2024, 1, 16);
        var date3 = new DateTime(2024, 1, 17);

        // Create logs for different days
        var logs1 = new List<ErrorLog>
        {
            new ErrorLog { Id = "day1-log1", Timestamp = date1.AddHours(10), Source = "App1", Message = "Day 1 Error 1" },
            new ErrorLog { Id = "day1-log2", Timestamp = date1.AddHours(14), Source = "App1", Message = "Day 1 Error 2" }
        };

        var logs2 = new List<ErrorLog>
        {
            new ErrorLog { Id = "day2-log1", Timestamp = date2.AddHours(9), Source = "App2", Message = "Day 2 Error 1" }
        };

        var logs3 = new List<ErrorLog>
        {
            new ErrorLog { Id = "day3-log1", Timestamp = date3.AddHours(16), Source = "App3", Message = "Day 3 Error 1" }
        };

        // Save logs to daily files
        await _jsonFileManager.SaveLogsAsync(_jsonFileManager.GetDailyLogFilePath(date1), logs1);
        await _jsonFileManager.SaveLogsAsync(_jsonFileManager.GetDailyLogFilePath(date2), logs2);
        await _jsonFileManager.SaveLogsAsync(_jsonFileManager.GetDailyLogFilePath(date3), logs3);

        // Act
        var result = await _jsonFileManager.LoadLogsByDateRangeAsync(date1, date2.AddHours(23).AddMinutes(59));

        // Assert
        Assert.Equal(3, result.Count); // Should include logs from day1 and day2, but not day3
        Assert.Contains(result, log => log.Id == "day1-log1");
        Assert.Contains(result, log => log.Id == "day1-log2");
        Assert.Contains(result, log => log.Id == "day2-log1");
        Assert.DoesNotContain(result, log => log.Id == "day3-log1");
    }

    [Fact]
    public async Task LoadLogsByDateRangeAsync_WithTimeFiltering_ShouldFilterByTimeRange()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15);
        var logs = new List<ErrorLog>
        {
            new ErrorLog { Id = "early-log", Timestamp = date.AddHours(8), Source = "App", Message = "Early Error" },
            new ErrorLog { Id = "mid-log", Timestamp = date.AddHours(12), Source = "App", Message = "Mid Error" },
            new ErrorLog { Id = "late-log", Timestamp = date.AddHours(18), Source = "App", Message = "Late Error" }
        };

        await _jsonFileManager.SaveLogsAsync(_jsonFileManager.GetDailyLogFilePath(date), logs);

        // Act - Load logs between 10 AM and 4 PM
        var fromTime = date.AddHours(10);
        var toTime = date.AddHours(16);
        var result = await _jsonFileManager.LoadLogsByDateRangeAsync(fromTime, toTime);

        // Assert
        Assert.Single(result); // Should only include the mid-day log
        Assert.Equal("mid-log", result[0].Id);
    }

    [Fact]
    public async Task SaveLogsAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var subDirectory = "subdirectory";
        var filePath = Path.Combine(subDirectory, "test-file.json");
        var logs = new List<ErrorLog>
        {
            new ErrorLog { Id = "test", Timestamp = DateTime.UtcNow, Source = "Test", Message = "Test" }
        };

        // Act
        await _jsonFileManager.SaveLogsAsync(filePath, logs);

        // Assert
        var fullPath = Path.Combine(_testDirectory, filePath);
        Assert.True(File.Exists(fullPath));
        
        var loadedLogs = await _jsonFileManager.LoadLogsAsync(filePath);
        Assert.Single(loadedLogs);
        Assert.Equal("test", loadedLogs[0].Id);
    }

    [Fact]
    public async Task LoadLogsAsync_CorruptedJsonFile_ShouldReturnEmptyListAndLogError()
    {
        // Arrange
        var filePath = "corrupted-file.json";
        var fullPath = Path.Combine(_testDirectory, filePath);
        await File.WriteAllTextAsync(fullPath, "{ invalid json content");

        // Act
        var result = await _jsonFileManager.LoadLogsAsync(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        
        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error loading logs from file")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}