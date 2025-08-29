using ErrorLogPrioritization.Api.Models;
using ErrorLogPrioritization.Api.Services;
using ErrorLogPrioritization.Api.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ErrorLogPrioritization.Api.Tests;

public class ScheduledAnalysisIntegrationTests
{
    private readonly Mock<IJsonFileManager> _mockJsonFileManager;
    private readonly Mock<ICopilotStudioService> _mockCopilotStudioService;
    private readonly Mock<ILogger<ScheduledAnalysisService>> _mockLogger;
    private readonly ScheduledAnalysisService _service;

    public ScheduledAnalysisIntegrationTests()
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
    public async Task CompleteDailyAnalysisWorkflow_WithHistoricalContext_ShouldProcessSuccessfully()
    {
        // Arrange
        var testDate = DateTime.UtcNow.Date.AddDays(-1);
        var currentLogs = new List<ErrorLog>
        {
            new ErrorLog
            {
                Id = "current-1",
                Timestamp = testDate,
                Message = "New error occurred",
                Source = "TestApp",
                IsAnalyzed = false
            },
            new ErrorLog
            {
                Id = "current-2",
                Timestamp = testDate,
                Message = "Database connection failed",
                Source = "TestApp",
                IsAnalyzed = false
            }
        };

        var historicalLogs = new List<ErrorLog>
        {
            new ErrorLog
            {
                Id = "historical-1",
                Timestamp = testDate.AddDays(-2),
                Message = "Database connection failed",
                Source = "TestApp",
                Priority = "High",
                Severity = "Critical",
                IsAnalyzed = true,
                AiReasoning = "Critical database connectivity issue"
            },
            new ErrorLog
            {
                Id = "historical-2",
                Timestamp = testDate.AddDays(-3),
                Message = "Authentication failed",
                Source = "TestApp",
                Priority = "Medium",
                Severity = "High",
                IsAnalyzed = true,
                AiReasoning = "User authentication issue"
            }
        };

        var analysisResponse = new CopilotAnalysisResponse
        {
            AnalyzedLogs = new List<AnalyzedLog>
            {
                new AnalyzedLog
                {
                    LogId = "current-1",
                    Severity = "Medium",
                    Priority = "Medium",
                    Reasoning = "New error pattern detected",
                    ConfidenceScore = 0.85
                },
                new AnalyzedLog
                {
                    LogId = "current-2",
                    Severity = "Critical",
                    Priority = "High",
                    Reasoning = "Known critical database issue pattern",
                    ConfidenceScore = 0.95
                }
            },
            OverallAssessment = "Critical database connectivity issues detected",
            Recommendations = new List<string> { "Check database server status", "Review connection strings" }
        };

        var processedLogs = new List<ErrorLog>
        {
            new ErrorLog
            {
                Id = "current-1",
                Timestamp = testDate,
                Message = "New error occurred",
                Source = "TestApp",
                Priority = "Medium",
                Severity = "Medium",
                IsAnalyzed = true,
                AiReasoning = "New error pattern detected",
                AnalyzedAt = DateTime.UtcNow
            },
            new ErrorLog
            {
                Id = "current-2",
                Timestamp = testDate,
                Message = "Database connection failed",
                Source = "TestApp",
                Priority = "High",
                Severity = "Critical",
                IsAnalyzed = true,
                AiReasoning = "Known critical database issue pattern",
                AnalyzedAt = DateTime.UtcNow
            }
        };

        // Setup mocks
        _mockJsonFileManager
            .Setup(x => x.GetDailyLogFilePath(testDate))
            .Returns("logs-current.json");

        _mockJsonFileManager
            .Setup(x => x.FileExists("logs-current.json"))
            .Returns(true);

        _mockJsonFileManager
            .Setup(x => x.LoadLogsAsync("logs-current.json"))
            .ReturnsAsync(currentLogs);

        // Setup historical context loading
        _mockJsonFileManager
            .Setup(x => x.LoadLogsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(historicalLogs);

        // Setup Copilot Studio service calls
        _mockCopilotStudioService
            .Setup(x => x.AnalyzeLogsAsync(
                It.Is<List<ErrorLog>>(logs => logs.Count == 2 && logs.All(l => !l.IsAnalyzed)),
                It.Is<HistoricalContext>(ctx => ctx.PreviousAnalysisResults.Count > 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisResponse);

        _mockCopilotStudioService
            .Setup(x => x.ProcessAnalysisResultsAsync(currentLogs, analysisResponse))
            .ReturnsAsync(processedLogs);

        _mockCopilotStudioService
            .Setup(x => x.UpdateLogsWithAnalysisAsync(processedLogs, "logs-current.json"))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ExecuteDailyAnalysisAsync();

        // Assert
        // Verify historical context was loaded
        _mockJsonFileManager.Verify(x => x.LoadLogsByDateRangeAsync(
            It.Is<DateTime>(d => d <= testDate.AddDays(-7)),
            It.Is<DateTime>(d => d >= testDate.AddDays(-1))), Times.Once);

        // Verify Copilot Studio analysis was called with historical context
        _mockCopilotStudioService.Verify(x => x.AnalyzeLogsAsync(
            It.Is<List<ErrorLog>>(logs => logs.Count == 2 && logs.All(l => !l.IsAnalyzed)),
            It.Is<HistoricalContext>(ctx => ctx.PreviousAnalysisResults.Count > 0),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify results were processed
        _mockCopilotStudioService.Verify(x => x.ProcessAnalysisResultsAsync(currentLogs, analysisResponse), Times.Once);

        // Verify logs were updated with analysis results
        _mockCopilotStudioService.Verify(x => x.UpdateLogsWithAnalysisAsync(processedLogs, "logs-current.json"), Times.Once);
    }

    [Fact]
    public async Task CompleteDailyAnalysisWorkflow_WithoutHistoricalContext_ShouldStillProcessSuccessfully()
    {
        // Arrange
        var testDate = DateTime.UtcNow.Date.AddDays(-1);
        var currentLogs = new List<ErrorLog>
        {
            new ErrorLog
            {
                Id = "current-1",
                Timestamp = testDate,
                Message = "First time error",
                Source = "TestApp",
                IsAnalyzed = false
            }
        };

        var analysisResponse = new CopilotAnalysisResponse
        {
            AnalyzedLogs = new List<AnalyzedLog>
            {
                new AnalyzedLog
                {
                    LogId = "current-1",
                    Severity = "Medium",
                    Priority = "Low",
                    Reasoning = "New error with low impact",
                    ConfidenceScore = 0.75
                }
            }
        };

        var processedLogs = new List<ErrorLog>
        {
            new ErrorLog
            {
                Id = "current-1",
                Timestamp = testDate,
                Message = "First time error",
                Source = "TestApp",
                Priority = "Low",
                Severity = "Medium",
                IsAnalyzed = true,
                AiReasoning = "New error with low impact",
                AnalyzedAt = DateTime.UtcNow
            }
        };

        // Setup mocks
        _mockJsonFileManager
            .Setup(x => x.GetDailyLogFilePath(testDate))
            .Returns("logs-current.json");

        _mockJsonFileManager
            .Setup(x => x.FileExists("logs-current.json"))
            .Returns(true);

        _mockJsonFileManager
            .Setup(x => x.LoadLogsAsync("logs-current.json"))
            .ReturnsAsync(currentLogs);

        // No historical logs available
        _mockJsonFileManager
            .Setup(x => x.LoadLogsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<ErrorLog>());

        _mockCopilotStudioService
            .Setup(x => x.AnalyzeLogsAsync(currentLogs, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisResponse);

        _mockCopilotStudioService
            .Setup(x => x.ProcessAnalysisResultsAsync(currentLogs, analysisResponse))
            .ReturnsAsync(processedLogs);

        _mockCopilotStudioService
            .Setup(x => x.UpdateLogsWithAnalysisAsync(processedLogs, "logs-current.json"))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ExecuteDailyAnalysisAsync();

        // Assert
        // Verify analysis still proceeded without historical context
        _mockCopilotStudioService.Verify(x => x.AnalyzeLogsAsync(currentLogs, null, It.IsAny<CancellationToken>()), Times.Once);
        _mockCopilotStudioService.Verify(x => x.ProcessAnalysisResultsAsync(currentLogs, analysisResponse), Times.Once);
        _mockCopilotStudioService.Verify(x => x.UpdateLogsWithAnalysisAsync(processedLogs, "logs-current.json"), Times.Once);
    }

    [Fact]
    public async Task CompleteDailyAnalysisWorkflow_WhenCopilotStudioFails_ShouldHandleGracefully()
    {
        // Arrange
        var testDate = DateTime.UtcNow.Date.AddDays(-1);
        var currentLogs = new List<ErrorLog>
        {
            new ErrorLog { Id = "current-1", Timestamp = testDate, IsAnalyzed = false }
        };

        _mockJsonFileManager
            .Setup(x => x.GetDailyLogFilePath(testDate))
            .Returns("logs-current.json");

        _mockJsonFileManager
            .Setup(x => x.FileExists("logs-current.json"))
            .Returns(true);

        _mockJsonFileManager
            .Setup(x => x.LoadLogsAsync("logs-current.json"))
            .ReturnsAsync(currentLogs);

        _mockJsonFileManager
            .Setup(x => x.LoadLogsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<ErrorLog>());

        _mockCopilotStudioService
            .Setup(x => x.AnalyzeLogsAsync(It.IsAny<List<ErrorLog>>(), It.IsAny<HistoricalContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Copilot Studio service unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _service.ExecuteDailyAnalysisAsync());

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HTTP error occurred during Copilot Studio analysis")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}