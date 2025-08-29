using ErrorLogPrioritization.Api.Models;
using ErrorLogPrioritization.Api.Services;
using ErrorLogPrioritization.Api.Utils;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace ErrorLogPrioritization.Api.Tests
{
    /// <summary>
    /// Load testing to ensure system meets performance requirements with 10,000+ log entries
    /// </summary>
    public class LoadTestingTests : IDisposable
    {
        private readonly Mock<IJsonFileManager> _mockJsonFileManager;
        private readonly Mock<ICopilotStudioService> _mockCopilotService;
        private readonly Mock<ILogger<LogService>> _mockLogServiceLogger;
        private readonly Mock<ILogger<ScheduledAnalysisService>> _mockScheduledLogger;
        private readonly LogService _logService;
        private readonly ScheduledAnalysisService _scheduledAnalysisService;
        private readonly string _testDataPath;

        public LoadTestingTests()
        {
            _mockJsonFileManager = new Mock<IJsonFileManager>();
            _mockCopilotService = new Mock<ICopilotStudioService>();
            _mockLogServiceLogger = new Mock<ILogger<LogService>>();
            _mockScheduledLogger = new Mock<ILogger<ScheduledAnalysisService>>();
            
            _testDataPath = Path.Combine(Path.GetTempPath(), "LoadTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDataPath);

            _logService = new LogService(_mockJsonFileManager.Object, _mockLogServiceLogger.Object);
            _scheduledAnalysisService = new ScheduledAnalysisService(
                _mockJsonFileManager.Object,
                _mockCopilotService.Object,
                _mockScheduledLogger.Object,
                Mock.Of<Hangfire.IBackgroundJobClient>());

            SetupMockServices();
        }

        [Fact]
        public void CreateLargeDataset_10000Logs_ShouldCompleteWithinTimeLimit()
        {
            // Arrange & Act
            var stopwatch = Stopwatch.StartNew();
            var largeBatch = CreateLargeTestDataset(10000);
            stopwatch.Stop();

            // Assert
            Assert.Equal(10000, largeBatch.Count);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                $"Creating 10,000 logs took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
            
            // Verify data integrity
            Assert.All(largeBatch, log => 
            {
                Assert.NotNull(log.Id);
                Assert.NotNull(log.Source);
                Assert.NotNull(log.Message);
                Assert.True(log.Timestamp > DateTime.MinValue);
                Assert.NotNull(log.StackTrace);
                Assert.NotNull(log.Severity);
            });

            // Verify unique IDs
            var uniqueIds = largeBatch.Select(l => l.Id).Distinct().Count();
            Assert.Equal(10000, uniqueIds);
        }

        [Fact]
        public async Task JsonSerialization_10000Logs_ShouldMeetPerformanceRequirements()
        {
            // Arrange
            var largeBatch = CreateLargeTestDataset(10000);
            var jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false // Optimize for performance
            };

            // Act - Serialization
            var serializationStopwatch = Stopwatch.StartNew();
            var json = JsonSerializer.Serialize(largeBatch, jsonOptions);
            serializationStopwatch.Stop();

            // Act - Deserialization
            var deserializationStopwatch = Stopwatch.StartNew();
            var deserializedLogs = JsonSerializer.Deserialize<List<ErrorLog>>(json, jsonOptions);
            deserializationStopwatch.Stop();

            // Assert
            Assert.True(serializationStopwatch.ElapsedMilliseconds < 10000, 
                $"Serializing 10,000 logs took {serializationStopwatch.ElapsedMilliseconds}ms, expected < 10000ms");
            
            Assert.True(deserializationStopwatch.ElapsedMilliseconds < 10000, 
                $"Deserializing 10,000 logs took {deserializationStopwatch.ElapsedMilliseconds}ms, expected < 10000ms");

            Assert.NotNull(deserializedLogs);
            Assert.Equal(10000, deserializedLogs.Count);
            
            // Verify data integrity after serialization round-trip
            for (int i = 0; i < Math.Min(100, largeBatch.Count); i++) // Sample check
            {
                Assert.Equal(largeBatch[i].Id, deserializedLogs[i].Id);
                Assert.Equal(largeBatch[i].Message, deserializedLogs[i].Message);
                Assert.Equal(largeBatch[i].Severity, deserializedLogs[i].Severity);
            }
        }

        [Fact]
        public async Task LogCollection_10000Logs_ShouldProcessWithinTimeLimit()
        {
            // Arrange
            var largeBatch = CreateLargeTestDataset(10000);
            
            // Setup mock to simulate file operations
            _mockJsonFileManager.Setup(x => x.AppendLogAsync(It.IsAny<string>(), It.IsAny<ErrorLog>()))
                .Returns(Task.CompletedTask);
            
            _mockJsonFileManager.Setup(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()))
                .Returns((DateTime date) => Path.Combine(_testDataPath, $"logs-{date:yyyy-MM-dd}.json"));

            // Act
            var stopwatch = Stopwatch.StartNew();
            
            foreach (var log in largeBatch)
            {
                await _logService.CollectLogsAsync(new List<ErrorLog> { log });
            }
            
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 60000, 
                $"Processing 10,000 logs took {stopwatch.ElapsedMilliseconds}ms, expected < 60000ms");
            
            // Verify all logs were processed
            _mockJsonFileManager.Verify(x => x.AppendLogAsync(It.IsAny<string>(), It.IsAny<ErrorLog>()), 
                Times.Exactly(10000));
        }

        [Fact]
        public async Task LogRetrieval_10000Logs_ShouldMeetPerformanceRequirements()
        {
            // Arrange
            var largeBatch = CreateLargeTestDataset(10000);
            
            _mockJsonFileManager.Setup(x => x.LoadLogsAsync(It.IsAny<string>()))
                .ReturnsAsync(largeBatch);
            
            _mockJsonFileManager.Setup(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()))
                .Returns((DateTime date) => Path.Combine(_testDataPath, $"logs-{date:yyyy-MM-dd}.json"));
            
            _mockJsonFileManager.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(true);

            // Act
            var stopwatch = Stopwatch.StartNew();
            var retrievedLogs = await _logService.GetFilteredLogsAsync(
                DateTime.Today.AddDays(-1),
                DateTime.Today);
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 15000, 
                $"Retrieving 10,000 logs took {stopwatch.ElapsedMilliseconds}ms, expected < 15000ms");
            
            Assert.NotNull(retrievedLogs);
            Assert.Equal(10000, retrievedLogs.Count);
        }

        [Fact]
        public async Task LogFiltering_10000Logs_ShouldPerformEfficiently()
        {
            // Arrange
            var largeBatch = CreateLargeTestDataset(10000);
            
            _mockJsonFileManager.Setup(x => x.LoadLogsAsync(It.IsAny<string>()))
                .ReturnsAsync(largeBatch);
            
            _mockJsonFileManager.Setup(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()))
                .Returns((DateTime date) => Path.Combine(_testDataPath, $"logs-{date:yyyy-MM-dd}.json"));
            
            _mockJsonFileManager.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(true);

            // Test different filtering scenarios
            var filterTests = new[]
            {
                new { Name = "Severity Filter", Severity = "Critical", Priority = (string?)null },
                new { Name = "Priority Filter", Severity = (string?)null, Priority = "High" },
                new { Name = "Combined Filter", Severity = "High", Priority = (string?)"Medium" }
            };

            foreach (var test in filterTests)
            {
                // Act
                var stopwatch = Stopwatch.StartNew();
                var filteredLogs = await _logService.GetFilteredLogsAsync(
                    DateTime.Today.AddDays(-1),
                    DateTime.Today,
                    severity: test.Severity,
                    priority: test.Priority);
                stopwatch.Stop();

                // Assert
                Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
                    $"{test.Name} on 10,000 logs took {stopwatch.ElapsedMilliseconds}ms, expected < 10000ms");
                
                Assert.NotNull(filteredLogs);
                
                if (test.Severity != null)
                {
                    Assert.All(filteredLogs, log => Assert.Equal(test.Severity, log.Severity));
                }
                
                if (test.Priority != null)
                {
                    Assert.All(filteredLogs, log => Assert.Equal(test.Priority, log.Priority));
                }
            }
        }

        [Fact]
        public async Task CopilotStudioAnalysis_LargeBatch_ShouldHandleEfficiently()
        {
            // Arrange
            var largeBatch = CreateLargeTestDataset(1000); // Smaller batch for AI analysis
            var mockResponse = CreateMockCopilotResponse(largeBatch);
            
            _mockCopilotService.Setup(x => x.AnalyzeLogsAsync(
                It.IsAny<List<ErrorLog>>(),
                It.IsAny<HistoricalContext>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            _mockCopilotService.Setup(x => x.ProcessAnalysisResultsAsync(
                It.IsAny<List<ErrorLog>>(),
                It.IsAny<CopilotAnalysisResponse>()))
                .ReturnsAsync((List<ErrorLog> logs, CopilotAnalysisResponse response) =>
                {
                    foreach (var log in logs)
                    {
                        var analysis = response.AnalyzedLogs.FirstOrDefault(a => a.LogId == log.Id);
                        if (analysis != null)
                        {
                            log.Priority = analysis.Priority;
                            log.AiReasoning = analysis.Reasoning;
                            log.IsAnalyzed = true;
                            log.AnalyzedAt = DateTime.UtcNow;
                        }
                    }
                    return logs;
                });

            // Act
            var stopwatch = Stopwatch.StartNew();
            var analyzedLogs = await _mockCopilotService.Object.ProcessAnalysisResultsAsync(largeBatch, mockResponse);
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 30000, 
                $"Processing analysis for 1,000 logs took {stopwatch.ElapsedMilliseconds}ms, expected < 30000ms");
            
            Assert.NotNull(analyzedLogs);
            Assert.Equal(1000, analyzedLogs.Count);
            Assert.All(analyzedLogs, log => Assert.True(log.IsAnalyzed));
        }

        [Fact]
        public async Task MemoryUsage_10000Logs_ShouldStayWithinLimits()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var largeBatch = CreateLargeTestDataset(10000);

            // Act - Simulate processing
            var processedLogs = new List<ErrorLog>();
            
            for (int i = 0; i < largeBatch.Count; i += 100) // Process in batches
            {
                var batch = largeBatch.Skip(i).Take(100).ToList();
                
                // Simulate JSON serialization/deserialization
                var json = JsonSerializer.Serialize(batch);
                var deserializedBatch = JsonSerializer.Deserialize<List<ErrorLog>>(json);
                
                processedLogs.AddRange(deserializedBatch ?? new List<ErrorLog>());
                
                // Force garbage collection every 1000 logs
                if (i % 1000 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            var finalMemory = GC.GetTotalMemory(true);
            var memoryUsed = finalMemory - initialMemory;

            // Assert
            Assert.Equal(10000, processedLogs.Count);
            
            // Memory usage should be reasonable (less than 100MB for 10k logs)
            var memoryUsedMB = memoryUsed / (1024.0 * 1024.0);
            Assert.True(memoryUsedMB < 100, 
                $"Memory usage was {memoryUsedMB:F2}MB, expected < 100MB");
        }

        [Fact]
        public async Task ConcurrentAccess_MultipleOperations_ShouldHandleCorrectly()
        {
            // Arrange
            var largeBatch = CreateLargeTestDataset(1000);
            var tasks = new List<Task>();
            
            _mockJsonFileManager.Setup(x => x.LoadLogsAsync(It.IsAny<string>()))
                .ReturnsAsync(largeBatch);
            
            _mockJsonFileManager.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(true);
            
            _mockJsonFileManager.Setup(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()))
                .Returns((DateTime date) => Path.Combine(_testDataPath, $"logs-{date:yyyy-MM-dd}.json"));

            // Act - Simulate concurrent read operations
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var logs = await _logService.GetFilteredLogsAsync(
                        DateTime.Today.AddDays(-1),
                        DateTime.Today);
                    
                    Assert.NotNull(logs);
                    Assert.True(logs.Count > 0);
                }));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 20000, 
                $"10 concurrent operations took {stopwatch.ElapsedMilliseconds}ms, expected < 20000ms");
        }

        [Fact]
        public void DataIntegrity_LargeDataset_ShouldMaintainConsistency()
        {
            // Arrange
            var largeBatch = CreateLargeTestDataset(10000);
            
            // Act - Simulate multiple processing operations
            var processedBatches = new List<List<ErrorLog>>();
            
            for (int i = 0; i < largeBatch.Count; i += 1000)
            {
                var batch = largeBatch.Skip(i).Take(1000).ToList();
                
                // Simulate analysis processing
                foreach (var log in batch)
                {
                    log.IsAnalyzed = true;
                    log.Priority = "Medium";
                    log.AiReasoning = "Load test analysis";
                    log.AnalyzedAt = DateTime.UtcNow;
                }
                
                processedBatches.Add(batch);
            }

            var allProcessedLogs = processedBatches.SelectMany(b => b).ToList();

            // Assert
            Assert.Equal(10000, allProcessedLogs.Count);
            
            // Verify all logs are marked as analyzed
            Assert.All(allProcessedLogs, log => Assert.True(log.IsAnalyzed));
            
            // Verify no duplicate IDs
            var uniqueIds = allProcessedLogs.Select(l => l.Id).Distinct().Count();
            Assert.Equal(10000, uniqueIds);
            
            // Verify all required fields are populated
            Assert.All(allProcessedLogs, log =>
            {
                Assert.NotNull(log.Id);
                Assert.NotNull(log.Source);
                Assert.NotNull(log.Message);
                Assert.NotNull(log.Priority);
                Assert.NotNull(log.AiReasoning);
                Assert.NotNull(log.AnalyzedAt);
            });
        }

        private void SetupMockServices()
        {
            _mockJsonFileManager.Setup(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()))
                .Returns((DateTime date) => Path.Combine(_testDataPath, $"logs-{date:yyyy-MM-dd}.json"));
            
            _mockJsonFileManager.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(true);
        }

        private List<ErrorLog> CreateLargeTestDataset(int count)
        {
            var logs = new List<ErrorLog>(count);
            var random = new Random(42); // Fixed seed for reproducible tests
            
            var sources = new[]
            {
                "LoadTest.Controllers.AuthController",
                "LoadTest.Controllers.UserController", 
                "LoadTest.Controllers.OrderController",
                "LoadTest.Services.DatabaseService",
                "LoadTest.Services.CacheService",
                "LoadTest.Services.EmailService",
                "LoadTest.Utils.ConfigHelper",
                "LoadTest.Utils.LoggingHelper",
                "LoadTest.Models.UserModel",
                "LoadTest.Models.OrderModel"
            };
            
            var severities = new[] { "Critical", "High", "Medium", "Low" };
            var severityWeights = new[] { 0.05, 0.15, 0.50, 0.30 }; // Distribution weights
            
            var messageTemplates = new[]
            {
                "Null reference exception occurred in {0}",
                "Database connection failed: {0}",
                "File not found: {0}",
                "Invalid operation attempted: {0}",
                "Timeout occurred during {0}",
                "Memory allocation failed for {0}",
                "Network connection lost while {0}",
                "Authentication failed for user {0}",
                "Authorization denied for resource {0}",
                "Configuration error detected in {0}",
                "Validation failed for input {0}",
                "Cache miss for key {0}",
                "Rate limit exceeded for {0}",
                "Service unavailable: {0}",
                "Data corruption detected in {0}"
            };

            for (int i = 0; i < count; i++)
            {
                var source = sources[random.Next(sources.Length)];
                var messageTemplate = messageTemplates[random.Next(messageTemplates.Length)];
                var severity = GetWeightedRandomSeverity(random, severities, severityWeights);
                
                logs.Add(new ErrorLog
                {
                    Id = $"load-test-{i:D6}-{Guid.NewGuid()}",
                    Timestamp = DateTime.Today.AddMinutes(random.Next(1440)), // Random time within day
                    Source = source,
                    Message = string.Format(messageTemplate, $"operation_{i}"),
                    StackTrace = GenerateStackTrace(source, random),
                    Severity = severity,
                    IsAnalyzed = false
                });
            }

            return logs;
        }

        private string GetWeightedRandomSeverity(Random random, string[] severities, double[] weights)
        {
            var randomValue = random.NextDouble();
            var cumulativeWeight = 0.0;
            
            for (int i = 0; i < severities.Length; i++)
            {
                cumulativeWeight += weights[i];
                if (randomValue <= cumulativeWeight)
                {
                    return severities[i];
                }
            }
            
            return severities[severities.Length - 1];
        }

        private string GenerateStackTrace(string source, Random random)
        {
            var methods = new[] { "Execute", "Process", "Handle", "Validate", "Transform", "Save", "Load" };
            var method = methods[random.Next(methods.Length)];
            var lineNumber = random.Next(1, 500);
            
            return $"at {source}.{method}() line {lineNumber}\n" +
                   $"at {source}.InternalMethod() line {lineNumber - 5}\n" +
                   $"at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification()";
        }

        private CopilotAnalysisResponse CreateMockCopilotResponse(List<ErrorLog> logs)
        {
            var analyzedLogs = logs.Select(log => new AnalyzedLog
            {
                LogId = log.Id,
                Severity = log.Severity,
                Priority = DeterminePriority(log.Severity),
                Reasoning = $"Analysis for {log.Severity} severity error in {log.Source}",
                ConfidenceScore = 0.85
            }).ToList();

            return new CopilotAnalysisResponse
            {
                AnalyzedLogs = analyzedLogs,
                OverallAssessment = $"Analyzed {logs.Count} logs with various severity levels",
                Recommendations = new List<string>
                {
                    "Monitor critical errors closely",
                    "Review high-frequency error patterns",
                    "Consider implementing additional error handling"
                }
            };
        }

        private string DeterminePriority(string severity)
        {
            return severity switch
            {
                "Critical" => "High",
                "High" => "High",
                "Medium" => "Medium",
                "Low" => "Low",
                _ => "Medium"
            };
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDataPath))
            {
                try
                {
                    Directory.Delete(_testDataPath, true);
                }
                catch (Exception)
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}