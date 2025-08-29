using ErrorLogPrioritization.Api.Models;
using ErrorLogPrioritization.Api.Services;
using ErrorLogPrioritization.Api.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ErrorLogPrioritization.Api.Tests
{
    public class CopilotStudioServiceTests
    {
        private readonly Mock<ILogger<CopilotStudioService>> _mockLogger;
        private readonly Mock<Utils.IJsonFileManager> _mockJsonFileManager;
        private readonly IConfiguration _configuration;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly CopilotStudioService _service;

        public CopilotStudioServiceTests()
        {
            _mockLogger = new Mock<ILogger<CopilotStudioService>>();
            _mockJsonFileManager = new Mock<Utils.IJsonFileManager>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            
            // Setup configuration
            _configuration = SetupConfiguration();
            
            _service = new CopilotStudioService(_httpClient, _mockLogger.Object, _configuration, _mockJsonFileManager.Object);
        }

        private IConfiguration SetupConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CopilotStudio:BaseUrl"] = "https://test-copilot-studio.com",
                ["CopilotStudio:ApiKey"] = "test-api-key",
                ["CopilotStudio:TimeoutSeconds"] = "30"
            });
            return configurationBuilder.Build();
        }

        [Fact]
        public async Task AnalyzeLogsAsync_WithValidLogs_ReturnsAnalysisResponse()
        {
            // Arrange
            var logs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow,
                    Message = "Test error message",
                    StackTrace = "Test stack trace",
                    Source = "TestApp"
                }
            };

            var expectedResponse = new CopilotAnalysisResponse
            {
                AnalyzedLogs = new List<AnalyzedLog>
                {
                    new AnalyzedLog
                    {
                        LogId = "1",
                        Severity = "High",
                        Priority = "Critical",
                        Reasoning = "Test reasoning",
                        ConfidenceScore = 0.95
                    }
                },
                OverallAssessment = "Critical issues found",
                Recommendations = new List<string> { "Fix immediately" },
                AnalysisTimestamp = DateTime.UtcNow
            };

            var responseJson = JsonSerializer.Serialize(expectedResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _service.AnalyzeLogsAsync(logs);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.AnalyzedLogs);
            Assert.Equal("1", result.AnalyzedLogs[0].LogId);
            Assert.Equal("High", result.AnalyzedLogs[0].Severity);
            Assert.Equal("Critical", result.AnalyzedLogs[0].Priority);
            Assert.Equal("Test reasoning", result.AnalyzedLogs[0].Reasoning);
            Assert.Equal(0.95, result.AnalyzedLogs[0].ConfidenceScore);
        }

        [Fact]
        public async Task AnalyzeLogsAsync_WithHttpError_ThrowsHttpRequestException()
        {
            // Arrange
            var logs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow,
                    Message = "Test error message",
                    Source = "TestApp"
                }
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Internal server error")
                });

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _service.AnalyzeLogsAsync(logs));
        }

        [Fact]
        public async Task AnalyzeLogsAsync_WithTimeout_ThrowsTimeoutException()
        {
            // Arrange
            var logs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow,
                    Message = "Test error message",
                    Source = "TestApp"
                }
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Request timeout", new TimeoutException()));

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutException>(() => _service.AnalyzeLogsAsync(logs));
        }

        [Fact]
        public async Task BuildAnalysisRequestAsync_WithValidLogs_ReturnsCorrectRequest()
        {
            // Arrange
            var logs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow,
                    Message = "Test error message",
                    StackTrace = "Test stack trace",
                    Source = "TestApp"
                },
                new ErrorLog
                {
                    Id = "2",
                    Timestamp = DateTime.UtcNow,
                    Message = "Another error message",
                    StackTrace = "Another stack trace",
                    Source = "TestApp"
                }
            };

            // Act
            var result = await _service.BuildAnalysisRequestAsync(logs);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Logs.Count);
            Assert.Equal("1", result.Logs[0].Id);
            Assert.Equal("Test error message", result.Logs[0].Message);
            Assert.Equal("Test stack trace", result.Logs[0].StackTrace);
            Assert.Equal("TestApp", result.Logs[0].Source);
            
            Assert.NotNull(result.Context);
            Assert.NotNull(result.Parameters);
            Assert.True(result.Parameters.IncludeSeverityClassification);
            Assert.True(result.Parameters.IncludePriorityAssignment);
            Assert.True(result.Parameters.IncludeReasoningExplanation);
            Assert.Equal(30, result.Parameters.MaxResponseTimeSeconds);
        }

        [Fact]
        public async Task BuildAnalysisRequestAsync_WithCustomContext_UsesProvidedContext()
        {
            // Arrange
            var logs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow,
                    Message = "Test error message",
                    Source = "TestApp"
                }
            };

            var customContext = new HistoricalContext
            {
                PreviousAnalyses = new List<string> { "Previous analysis 1" },
                FrequentErrors = new List<string> { "Frequent error 1" },
                ResolvedIssues = new List<string> { "Resolved issue 1" }
            };

            // Act
            var result = await _service.BuildAnalysisRequestAsync(logs, customContext);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(customContext, result.Context);
            Assert.Single(result.Context.PreviousAnalyses);
            Assert.Equal("Previous analysis 1", result.Context.PreviousAnalyses[0]);
        }

        [Fact]
        public async Task BuildAnalysisRequestAsync_WithEmptyLogs_ReturnsEmptyRequest()
        {
            // Arrange
            var logs = new List<ErrorLog>();

            // Act
            var result = await _service.BuildAnalysisRequestAsync(logs);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Logs);
            Assert.NotNull(result.Context);
            Assert.NotNull(result.Parameters);
        }

        [Fact]
        public async Task BuildAnalysisRequestAsync_WithSimilarErrors_IdentifiesFrequentErrors()
        {
            // Arrange
            var logs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow,
                    Message = "Database connection failed with ID 123",
                    Source = "TestApp"
                },
                new ErrorLog
                {
                    Id = "2",
                    Timestamp = DateTime.UtcNow,
                    Message = "Database connection failed with ID 456",
                    Source = "TestApp"
                },
                new ErrorLog
                {
                    Id = "3",
                    Timestamp = DateTime.UtcNow,
                    Message = "File not found error",
                    Source = "TestApp"
                }
            };

            // Act
            var result = await _service.BuildAnalysisRequestAsync(logs);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Context);
            Assert.NotEmpty(result.Context.FrequentErrors);
            
            // Should identify the database connection error pattern as frequent
            var frequentError = result.Context.FrequentErrors.FirstOrDefault(e => e.Contains("Database connection failed"));
            Assert.NotNull(frequentError);
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            var service = new CopilotStudioService(_httpClient, _mockLogger.Object, _configuration, _mockJsonFileManager.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public async Task AnalyzeLogsAsync_LogsInformationMessages()
        {
            // Arrange
            var logs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow,
                    Message = "Test error message",
                    Source = "TestApp"
                }
            };

            var expectedResponse = new CopilotAnalysisResponse
            {
                AnalyzedLogs = new List<AnalyzedLog>(),
                OverallAssessment = "No issues",
                Recommendations = new List<string>(),
                AnalysisTimestamp = DateTime.UtcNow
            };

            var responseJson = JsonSerializer.Serialize(expectedResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });

            // Act
            await _service.AnalyzeLogsAsync(logs);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting Copilot Studio analysis")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Copilot Studio analysis completed successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessAnalysisResultsAsync_WithValidResults_UpdatesLogsCorrectly()
        {
            // Arrange
            var originalLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow,
                    Message = "Test error 1",
                    Source = "TestApp",
                    Severity = "Unknown",
                    Priority = "Unknown",
                    IsAnalyzed = false
                },
                new ErrorLog
                {
                    Id = "2",
                    Timestamp = DateTime.UtcNow,
                    Message = "Test error 2",
                    Source = "TestApp",
                    Severity = "Unknown",
                    Priority = "Unknown",
                    IsAnalyzed = false
                }
            };

            var analysisResponse = new CopilotAnalysisResponse
            {
                AnalyzedLogs = new List<AnalyzedLog>
                {
                    new AnalyzedLog
                    {
                        LogId = "1",
                        Severity = "High",
                        Priority = "High",
                        Reasoning = "Critical database error",
                        ConfidenceScore = 0.95
                    },
                    new AnalyzedLog
                    {
                        LogId = "2",
                        Severity = "Medium",
                        Priority = "Low",
                        Reasoning = "Minor UI issue",
                        ConfidenceScore = 0.80
                    }
                }
            };

            // Act
            var result = await _service.ProcessAnalysisResultsAsync(originalLogs, analysisResponse);

            // Assert
            Assert.Equal(2, result.Count);
            
            var log1 = result.First(l => l.Id == "1");
            Assert.Equal("High", log1.Severity);
            Assert.Equal("High", log1.Priority);
            Assert.Equal("Critical database error", log1.AiReasoning);
            Assert.True(log1.IsAnalyzed);
            Assert.NotNull(log1.AnalyzedAt);

            var log2 = result.First(l => l.Id == "2");
            Assert.Equal("Medium", log2.Severity);
            Assert.Equal("Low", log2.Priority);
            Assert.Equal("Minor UI issue", log2.AiReasoning);
            Assert.True(log2.IsAnalyzed);
            Assert.NotNull(log2.AnalyzedAt);
        }

        [Fact]
        public async Task ProcessAnalysisResultsAsync_WithInvalidResults_SkipsInvalidEntries()
        {
            // Arrange
            var originalLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow,
                    Message = "Test error 1",
                    Source = "TestApp",
                    Severity = "Unknown",
                    Priority = "Unknown",
                    IsAnalyzed = false
                }
            };

            var analysisResponse = new CopilotAnalysisResponse
            {
                AnalyzedLogs = new List<AnalyzedLog>
                {
                    new AnalyzedLog
                    {
                        LogId = "1",
                        Severity = "InvalidSeverity", // Invalid severity
                        Priority = "High",
                        Reasoning = "Test reasoning",
                        ConfidenceScore = 0.95
                    }
                }
            };

            // Act
            var result = await _service.ProcessAnalysisResultsAsync(originalLogs, analysisResponse);

            // Assert
            Assert.Single(result);
            var log = result.First();
            Assert.Equal("Unknown", log.Severity); // Should remain unchanged
            Assert.Equal("Unknown", log.Priority); // Should remain unchanged
            Assert.False(log.IsAnalyzed); // Should remain false
        }

        [Fact]
        public async Task ProcessAnalysisResultsAsync_WithEmptyResponse_ReturnsOriginalLogs()
        {
            // Arrange
            var originalLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow,
                    Message = "Test error 1",
                    Source = "TestApp"
                }
            };

            var analysisResponse = new CopilotAnalysisResponse
            {
                AnalyzedLogs = new List<AnalyzedLog>()
            };

            // Act
            var result = await _service.ProcessAnalysisResultsAsync(originalLogs, analysisResponse);

            // Assert
            Assert.Single(result);
            Assert.Equal(originalLogs[0].Id, result[0].Id);
            Assert.Equal(originalLogs[0].Message, result[0].Message);
        }

        [Fact]
        public async Task UpdateLogsWithAnalysisAsync_WithValidLogs_UpdatesFileCorrectly()
        {
            // Arrange
            var filePath = "test-logs.json";
            var existingLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow.AddHours(-1),
                    Message = "Existing error",
                    Source = "TestApp",
                    Severity = "Unknown",
                    Priority = "Unknown",
                    IsAnalyzed = false
                }
            };

            var analyzedLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow.AddHours(-1),
                    Message = "Existing error",
                    Source = "TestApp",
                    Severity = "High",
                    Priority = "High",
                    AiReasoning = "Critical issue",
                    IsAnalyzed = true,
                    AnalyzedAt = DateTime.UtcNow
                }
            };

            _mockJsonFileManager.Setup(x => x.LoadLogsAsync(filePath))
                .ReturnsAsync(existingLogs);

            _mockJsonFileManager.Setup(x => x.SaveLogsAsync(filePath, It.IsAny<List<ErrorLog>>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateLogsWithAnalysisAsync(analyzedLogs, filePath);

            // Assert
            _mockJsonFileManager.Verify(x => x.LoadLogsAsync(filePath), Times.Once);
            _mockJsonFileManager.Verify(x => x.SaveLogsAsync(filePath, It.Is<List<ErrorLog>>(logs =>
                logs.Count == 1 &&
                logs[0].Id == "1" &&
                logs[0].Severity == "High" &&
                logs[0].Priority == "High" &&
                logs[0].IsAnalyzed == true
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateLogsWithAnalysisAsync_WithNewLogs_AddsNewLogsToFile()
        {
            // Arrange
            var filePath = "test-logs.json";
            var existingLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    Message = "Existing error",
                    Source = "TestApp"
                }
            };

            var analyzedLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "2",
                    Timestamp = DateTime.UtcNow.AddHours(-1),
                    Message = "New error",
                    Source = "TestApp",
                    Severity = "Medium",
                    Priority = "Medium",
                    AiReasoning = "New issue",
                    IsAnalyzed = true,
                    AnalyzedAt = DateTime.UtcNow
                }
            };

            _mockJsonFileManager.Setup(x => x.LoadLogsAsync(filePath))
                .ReturnsAsync(existingLogs);

            _mockJsonFileManager.Setup(x => x.SaveLogsAsync(filePath, It.IsAny<List<ErrorLog>>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateLogsWithAnalysisAsync(analyzedLogs, filePath);

            // Assert
            _mockJsonFileManager.Verify(x => x.SaveLogsAsync(filePath, It.Is<List<ErrorLog>>(logs =>
                logs.Count == 2 &&
                logs.Any(l => l.Id == "1") &&
                logs.Any(l => l.Id == "2" && l.IsAnalyzed == true)
            )), Times.Once);
        }
    }
}