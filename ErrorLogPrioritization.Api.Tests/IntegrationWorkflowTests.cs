using ErrorLogPrioritization.Api.Models;
using ErrorLogPrioritization.Api.Services;
using ErrorLogPrioritization.Api.Utils;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace ErrorLogPrioritization.Api.Tests;

public class IntegrationWorkflowTests : IDisposable
{
    private readonly Mock<IJsonFileManager> _mockJsonFileManager;
    private readonly Mock<ICopilotStudioService> _mockCopilotService;
    private readonly Mock<ILogger<ScheduledAnalysisService>> _mockLogger;
    private readonly Mock<Hangfire.IBackgroundJobClient> _mockBackgroundJobClient;
    private readonly ScheduledAnalysisService _scheduledAnalysisService;
    private readonly string _testDataPath;

    public IntegrationWorkflowTests()
    {
        _mockJsonFileManager = new Mock<IJsonFileManager>();
        _mockCopilotService = new Mock<ICopilotStudioService>();
        _mockLogger = new Mock<ILogger<ScheduledAnalysisService>>();
        _mockBackgroundJobClient = new Mock<Hangfire.IBackgroundJobClient>();
        
        _testDataPath = Path.Combine(Path.GetTempPath(), "IntegrationTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataPath);

        _scheduledAnalysisService = new ScheduledAnalysisService(
            _mockJsonFileManager.Object,
            _mockCopilotService.Object,
            _mockLogger.Object,
            _mockBackgroundJobClient.Object);
    }

    [Fact]
    public async Task CompleteAnalysisWorkflow_WithMockServices_ShouldExecuteSuccessfully()
    {
        // Arrange
        var testLogs = CreateTestLogsForAnalysis();
        var mockAnalysisResponse = CreateMockCopilotAnalysisResponse();
        var analyzedLogs = CreateAnalyzedLogs();
        var previousDay = DateTime.Today.AddDays(-1);

        // Setup mock file manager
        _mockJsonFileManager.Setup(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()))
            .Returns("test-log-file.json");
        
        _mockJsonFileManager.Setup(x => x.FileExists(It.IsAny<string>()))
            .Returns(true);
            
        _mockJsonFileManager.Setup(x => x.LoadLogsAsync(It.IsAny<string>()))
            .ReturnsAsync(testLogs);

        // Setup mock Copilot Studio service
        _mockCopilotService.Setup(x => x.AnalyzeLogsAsync(
                It.IsAny<List<ErrorLog>>(), 
                It.IsAny<HistoricalContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAnalysisResponse);

        _mockCopilotService.Setup(x => x.ProcessAnalysisResultsAsync(
                It.IsAny<List<ErrorLog>>(), 
                It.IsAny<CopilotAnalysisResponse>()))
            .ReturnsAsync(analyzedLogs);

        _mockCopilotService.Setup(x => x.UpdateLogsWithAnalysisAsync(
                It.IsAny<List<ErrorLog>>(), 
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var exception = await Record.ExceptionAsync(async () => 
            await _scheduledAnalysisService.ExecuteDailyAnalysisAsync());

        // Assert
        Assert.Null(exception);
        
        _mockJsonFileManager.Verify(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()), Times.Once);
        _mockJsonFileManager.Verify(x => x.FileExists(It.IsAny<string>()), Times.Once);
        _mockJsonFileManager.Verify(x => x.LoadLogsAsync(It.IsAny<string>()), Times.Once);
        
        _mockCopilotService.Verify(x => x.AnalyzeLogsAsync(
            It.IsAny<List<ErrorLog>>(), It.IsAny<HistoricalContext>(), It.IsAny<CancellationToken>()), Times.Once);
        
        _mockCopilotService.Verify(x => x.ProcessAnalysisResultsAsync(
            It.IsAny<List<ErrorLog>>(), It.IsAny<CopilotAnalysisResponse>()), Times.Once);
        
        _mockCopilotService.Verify(x => x.UpdateLogsWithAnalysisAsync(
            It.IsAny<List<ErrorLog>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task AnalysisWorkflow_WithNoLogsToAnalyze_ShouldSkipProcessing()
    {
        // Arrange
        var emptyLogsList = new List<ErrorLog>();

        _mockJsonFileManager.Setup(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()))
            .Returns("test-log-file.json");
        
        _mockJsonFileManager.Setup(x => x.FileExists(It.IsAny<string>()))
            .Returns(false);

        // Act
        await _scheduledAnalysisService.ExecuteDailyAnalysisAsync();

        // Assert
        _mockJsonFileManager.Verify(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()), Times.Once);
        _mockJsonFileManager.Verify(x => x.FileExists(It.IsAny<string>()), Times.Once);
        
        // Should not call Copilot Studio if no logs to analyze
        _mockCopilotService.Verify(x => x.AnalyzeLogsAsync(
            It.IsAny<List<ErrorLog>>(), It.IsAny<HistoricalContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AnalysisWorkflow_WithCopilotStudioFailure_ShouldPropagateException()
    {
        // Arrange
        var testLogs = CreateTestLogsForAnalysis();

        _mockJsonFileManager.Setup(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()))
            .Returns("test-log-file.json");
        
        _mockJsonFileManager.Setup(x => x.FileExists(It.IsAny<string>()))
            .Returns(true);
            
        _mockJsonFileManager.Setup(x => x.LoadLogsAsync(It.IsAny<string>()))
            .ReturnsAsync(testLogs);

        _mockCopilotService.Setup(x => x.AnalyzeLogsAsync(
                It.IsAny<List<ErrorLog>>(), 
                It.IsAny<HistoricalContext>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Copilot Studio service unavailable"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _scheduledAnalysisService.ExecuteDailyAnalysisAsync());

        Assert.Contains("Copilot Studio service unavailable", exception.Message);
    }

    [Fact]
    public void LoadTesting_CreateLargeDataset_ShouldCompleteWithinTimeLimit()
    {
        // Arrange & Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var largeBatchOfLogs = CreateLargeTestDataset(10000);
        stopwatch.Stop();

        // Assert
        Assert.Equal(10000, largeBatchOfLogs.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                   $"Large dataset creation took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        
        // Verify data integrity
        Assert.All(largeBatchOfLogs, log => 
        {
            Assert.NotNull(log.Id);
            Assert.NotNull(log.Source);
            Assert.NotNull(log.Message);
            Assert.True(log.Timestamp > DateTime.MinValue);
        });
    }

    [Fact]
    public void JsonSerialization_WithComplexErrorLogs_ShouldSerializeCorrectly()
    {
        // Arrange
        var testLogs = CreateTestLogsWithAnalysis();

        // Act
        var json = JsonSerializer.Serialize(testLogs, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true 
        });
        
        var deserializedLogs = JsonSerializer.Deserialize<List<ErrorLog>>(json, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });

        // Assert
        Assert.NotNull(json);
        Assert.NotNull(deserializedLogs);
        Assert.Equal(testLogs.Count, deserializedLogs.Count);
        
        for (int i = 0; i < testLogs.Count; i++)
        {
            Assert.Equal(testLogs[i].Id, deserializedLogs[i].Id);
            Assert.Equal(testLogs[i].Message, deserializedLogs[i].Message);
            Assert.Equal(testLogs[i].Severity, deserializedLogs[i].Severity);
            Assert.Equal(testLogs[i].Priority, deserializedLogs[i].Priority);
            Assert.Equal(testLogs[i].IsAnalyzed, deserializedLogs[i].IsAnalyzed);
        }
    }

    [Fact]
    public void PerformanceTest_ProcessingLargeLogBatch_ShouldMeetRequirements()
    {
        // Arrange
        var largeBatch = CreateLargeTestDataset(1000);
        var mockResponse = CreateMockCopilotAnalysisResponse();

        // Act - Simulate processing time
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Simulate the processing that would happen in the real workflow
        var processedLogs = new List<ErrorLog>();
        foreach (var log in largeBatch)
        {
            var processedLog = new ErrorLog
            {
                Id = log.Id,
                Timestamp = log.Timestamp,
                Source = log.Source,
                Message = log.Message,
                StackTrace = log.StackTrace,
                Severity = log.Severity,
                Priority = "Medium", // Simulated AI assignment
                AiReasoning = "Automated analysis result",
                IsAnalyzed = true,
                AnalyzedAt = DateTime.UtcNow
            };
            processedLogs.Add(processedLog);
        }
        
        stopwatch.Stop();

        // Assert
        Assert.Equal(largeBatch.Count, processedLogs.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
                   $"Processing {largeBatch.Count} logs took {stopwatch.ElapsedMilliseconds}ms, expected < 10000ms");
        
        Assert.All(processedLogs, log => Assert.True(log.IsAnalyzed));
    }

    private List<ErrorLog> CreateTestLogsForAnalysis()
    {
        return new List<ErrorLog>
        {
            new ErrorLog
            {
                Id = "test-log-1",
                Timestamp = DateTime.Today.AddDays(-1).AddHours(10),
                Source = "TestApp.Controllers.AuthController",
                Message = "User authentication failed",
                StackTrace = "at AuthController.Login() line 45",
                Severity = "Error",
                IsAnalyzed = false
            },
            new ErrorLog
            {
                Id = "test-log-2",
                Timestamp = DateTime.Today.AddDays(-1).AddHours(14),
                Source = "TestApp.Services.DatabaseService",
                Message = "Database connection timeout",
                StackTrace = "at DatabaseService.Connect() line 23",
                Severity = "Critical",
                IsAnalyzed = false
            },
            new ErrorLog
            {
                Id = "test-log-3",
                Timestamp = DateTime.Today.AddDays(-1).AddHours(16),
                Source = "TestApp.Utils.ConfigHelper",
                Message = "Configuration file not found",
                StackTrace = "at ConfigHelper.LoadConfig() line 12",
                Severity = "Warning",
                IsAnalyzed = false
            }
        };
    }

    private List<ErrorLog> CreateTestLogsWithAnalysis()
    {
        var logs = CreateTestLogsForAnalysis();
        foreach (var log in logs)
        {
            log.IsAnalyzed = true;
            log.Priority = "High";
            log.AiReasoning = "Critical system component failure requiring immediate attention";
            log.AnalyzedAt = DateTime.UtcNow;
        }
        return logs;
    }

    private CopilotAnalysisResponse CreateMockCopilotAnalysisResponse()
    {
        return new CopilotAnalysisResponse
        {
            AnalyzedLogs = new List<AnalyzedLog>
            {
                new AnalyzedLog
                {
                    LogId = "test-log-1",
                    Severity = "High",
                    Priority = "High",
                    Reasoning = "Authentication failures can lead to security issues",
                    ConfidenceScore = 0.92
                },
                new AnalyzedLog
                {
                    LogId = "test-log-2",
                    Severity = "Critical",
                    Priority = "High",
                    Reasoning = "Database connectivity affects core application functionality",
                    ConfidenceScore = 0.98
                },
                new AnalyzedLog
                {
                    LogId = "test-log-3",
                    Severity = "Medium",
                    Priority = "Low",
                    Reasoning = "Configuration issues have fallback mechanisms",
                    ConfidenceScore = 0.75
                }
            },
            OverallAssessment = "Critical database issues require immediate attention",
            Recommendations = new List<string>
            {
                "Investigate database connection pool settings",
                "Review authentication error patterns",
                "Ensure configuration files are properly deployed"
            }
        };
    }

    private List<ErrorLog> CreateAnalyzedLogs()
    {
        var logs = CreateTestLogsForAnalysis();
        var analysisResponse = CreateMockCopilotAnalysisResponse();

        foreach (var log in logs)
        {
            var analysis = analysisResponse.AnalyzedLogs.FirstOrDefault(a => a.LogId == log.Id);
            if (analysis != null)
            {
                log.Severity = analysis.Severity;
                log.Priority = analysis.Priority;
                log.AiReasoning = analysis.Reasoning;
                log.IsAnalyzed = true;
                log.AnalyzedAt = DateTime.UtcNow;
            }
        }

        return logs;
    }

    private List<ErrorLog> CreateLargeTestDataset(int count)
    {
        var logs = new List<ErrorLog>();
        var random = new Random();
        var sources = new[] { "App.Controllers", "App.Services", "App.Utils", "App.Models" };
        var severities = new[] { "Critical", "Error", "Warning", "Information" };
        var messages = new[]
        {
            "Null reference exception",
            "Database connection failed",
            "File not found",
            "Invalid operation",
            "Timeout occurred"
        };

        for (int i = 0; i < count; i++)
        {
            logs.Add(new ErrorLog
            {
                Id = $"large-test-log-{i}",
                Timestamp = DateTime.Today.AddDays(-1).AddMinutes(random.Next(1440)),
                Source = sources[random.Next(sources.Length)],
                Message = messages[random.Next(messages.Length)],
                StackTrace = $"at {sources[random.Next(sources.Length)]}.Method() line {random.Next(1, 100)}",
                Severity = severities[random.Next(severities.Length)],
                IsAnalyzed = false
            });
        }

        return logs;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, true);
        }
    }
}