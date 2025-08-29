using ErrorLogPrioritization.Api.Models;
using ErrorLogPrioritization.Api.Services;
using ErrorLogPrioritization.Api.Utils;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ErrorLogPrioritization.Api.Tests;

public class ScheduledAnalysisServiceTests
{
    private readonly Mock<IJsonFileManager> _mockJsonFileManager;
    private readonly Mock<ICopilotStudioService> _mockCopilotStudioService;
    private readonly Mock<ILogger<ScheduledAnalysisService>> _mockLogger;
    private readonly ScheduledAnalysisService _service;

    public ScheduledAnalysisServiceTests()
    {
        _mockJsonFileManager = new Mock<IJsonFileManager>();
        _mockCopilotStudioService = new Mock<ICopilotStudioService>();
        _mockLogger = new Mock<ILogger<ScheduledAnalysisService>>();
        
        _service = new ScheduledAnalysisService(
            _mockJsonFileManager.Object,
            _mockCopilotStudioService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteDailyAnalysisAsync_WithLogsAvailable_ShouldProcessSuccessfully()
    {
        // Arrange
        var testDate = DateTime.UtcNow.Date.AddDays(-1);
        var testLogs = new List<ErrorLog>
        {
            new ErrorLog
            {
                Id = "1",
                Timestamp = testDate,
                Message = "Test error 1",
                IsAnalyzed = false
            },
            new ErrorLog
            {
                Id = "2",
                Timestamp = testDate,
                Message = "Test error 2",
                IsAnalyzed = false
            }
        };

        _mockJsonFileManager
            .Setup(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()))
            .Returns("test-path.json");

        _mockJsonFileManager
            .Setup(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()))
            .Returns("existing-file.json");

        _mockJsonFileManager
            .Setup(x => x.FileExists("existing-file.json"))
            .Returns(true);

        _mockJsonFileManager
            .Setup(x => x.LoadLogsAsync("existing-file.json"))
            .ReturnsAsync(testLogs);

        _mockJsonFileManager
            .Setup(x => x.LoadLogsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<ErrorLog>());

        _mockCopilotStudioService
            .Setup(x => x.AnalyzeLogsAsync(It.IsAny<List<ErrorLog>>(), It.IsAny<HistoricalContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CopilotAnalysisResponse { AnalyzedLogs = new List<AnalyzedLog>() });

        _mockCopilotStudioService
            .Setup(x => x.ProcessAnalysisResultsAsync(It.IsAny<List<ErrorLog>>(), It.IsAny<CopilotAnalysisResponse>()))
            .ReturnsAsync(testLogs);

        _mockCopilotStudioService
            .Setup(x => x.UpdateLogsWithAnalysisAsync(It.IsAny<List<ErrorLog>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ExecuteDailyAnalysisAsync();

        // Assert
        _mockCopilotStudioService.Verify(x => x.AnalyzeLogsAsync(It.Is<List<ErrorLog>>(logs => 
            logs.Count == 2 && logs.All(l => !l.IsAnalyzed)), It.IsAny<HistoricalContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteDailyAnalysisAsync_WithNoLogsAvailable_ShouldSkipAnalysis()
    {
        // Arrange
        _mockJsonFileManager
            .Setup(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()))
            .Returns("non-existing-file.json");

        _mockJsonFileManager
            .Setup(x => x.FileExists("non-existing-file.json"))
            .Returns(false);

        // Act
        await _service.ExecuteDailyAnalysisAsync();

        // Assert
        _mockCopilotStudioService.Verify(x => x.AnalyzeLogsAsync(It.IsAny<List<ErrorLog>>(), It.IsAny<HistoricalContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CollectPreviousDayLogsAsync_WithValidDate_ShouldReturnUnanalyzedLogs()
    {
        // Arrange
        var testDate = new DateTime(2024, 1, 15);
        var testLogs = new List<ErrorLog>
        {
            new ErrorLog { Id = "1", IsAnalyzed = false, Message = "Unanalyzed log" },
            new ErrorLog { Id = "2", IsAnalyzed = true, Message = "Already analyzed log" },
            new ErrorLog { Id = "3", IsAnalyzed = false, Message = "Another unanalyzed log" }
        };

        _mockJsonFileManager
            .Setup(x => x.GetDailyLogFilePath(testDate))
            .Returns("logs-2024-01-15.json");

        _mockJsonFileManager
            .Setup(x => x.FileExists("logs-2024-01-15.json"))
            .Returns(true);

        _mockJsonFileManager
            .Setup(x => x.LoadLogsAsync("logs-2024-01-15.json"))
            .ReturnsAsync(testLogs);

        // Act
        var result = await _service.CollectPreviousDayLogsAsync(testDate);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, log => Assert.False(log.IsAnalyzed));
    }

    [Fact]
    public async Task CollectPreviousDayLogsAsync_WithNonExistentFile_ShouldReturnEmptyList()
    {
        // Arrange
        var testDate = new DateTime(2024, 1, 15);
        
        _mockJsonFileManager
            .Setup(x => x.GetDailyLogFilePath(testDate))
            .Returns("non-existing-file.json");

        _mockJsonFileManager
            .Setup(x => x.FileExists("non-existing-file.json"))
            .Returns(false);

        // Act
        var result = await _service.CollectPreviousDayLogsAsync(testDate);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task TriggerCopilotAnalysisAsync_WithValidLogs_ShouldCallCopilotService()
    {
        // Arrange
        var testLogs = new List<ErrorLog>
        {
            new ErrorLog { Id = "1", Message = "Test error" }
        };

        _mockJsonFileManager
            .Setup(x => x.LoadLogsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<ErrorLog>());

        _mockCopilotStudioService
            .Setup(x => x.AnalyzeLogsAsync(testLogs, It.IsAny<HistoricalContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CopilotAnalysisResponse { AnalyzedLogs = new List<AnalyzedLog>() });

        _mockCopilotStudioService
            .Setup(x => x.ProcessAnalysisResultsAsync(It.IsAny<List<ErrorLog>>(), It.IsAny<CopilotAnalysisResponse>()))
            .ReturnsAsync(testLogs);

        _mockCopilotStudioService
            .Setup(x => x.UpdateLogsWithAnalysisAsync(It.IsAny<List<ErrorLog>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.TriggerCopilotAnalysisAsync(testLogs);

        // Assert
        _mockCopilotStudioService.Verify(x => x.AnalyzeLogsAsync(testLogs, It.IsAny<HistoricalContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TriggerCopilotAnalysisAsync_WhenCopilotServiceThrows_ShouldPropagateException()
    {
        // Arrange
        var testLogs = new List<ErrorLog>
        {
            new ErrorLog { Id = "1", Message = "Test error" }
        };

        _mockJsonFileManager
            .Setup(x => x.LoadLogsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<ErrorLog>());

        _mockCopilotStudioService
            .Setup(x => x.AnalyzeLogsAsync(testLogs, It.IsAny<HistoricalContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Copilot service error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.TriggerCopilotAnalysisAsync(testLogs));
    }

    [Fact]
    public async Task ExecuteDailyAnalysisAsync_WhenExceptionOccurs_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var expectedException = new Exception("File system error");
        _mockJsonFileManager
            .Setup(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()))
            .Throws(expectedException);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<Exception>(() => _service.ExecuteDailyAnalysisAsync());
        Assert.Equal(expectedException, actualException);
        
        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred during daily analysis execution")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}