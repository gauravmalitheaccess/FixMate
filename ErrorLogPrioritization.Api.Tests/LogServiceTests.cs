using ErrorLogPrioritization.Api.Models;
using ErrorLogPrioritization.Api.Services;
using ErrorLogPrioritization.Api.Utils;
using Microsoft.Extensions.Logging;
using Moq;

namespace ErrorLogPrioritization.Api.Tests
{
    public class LogServiceTests
    {
        private readonly Mock<IJsonFileManager> _mockJsonFileManager;
        private readonly Mock<ILogger<LogService>> _mockLogger;
        private readonly LogService _logService;

        public LogServiceTests()
        {
            _mockJsonFileManager = new Mock<IJsonFileManager>();
            _mockLogger = new Mock<ILogger<LogService>>();
            _logService = new LogService(_mockJsonFileManager.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CollectLogsAsync_WithValidLogs_ReturnsTrue()
        {
            // Arrange
            var logs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow,
                    Source = "TestApp",
                    Message = "Test error message",
                    Severity = "High"
                },
                new ErrorLog
                {
                    Id = "2",
                    Timestamp = DateTime.UtcNow,
                    Source = "TestApp",
                    Message = "Another test error",
                    Severity = "Medium"
                }
            };

            _mockJsonFileManager.Setup(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()))
                .Returns("logs-2024-01-01.json");
            _mockJsonFileManager.Setup(x => x.LoadLogsAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<ErrorLog>());
            _mockJsonFileManager.Setup(x => x.SaveLogsAsync(It.IsAny<string>(), It.IsAny<List<ErrorLog>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _logService.CollectLogsAsync(logs);

            // Assert
            Assert.True(result);
            _mockJsonFileManager.Verify(x => x.SaveLogsAsync(It.IsAny<string>(), It.IsAny<List<ErrorLog>>()), Times.Once);
        }

        [Fact]
        public async Task CollectLogsAsync_WithNullLogs_ReturnsFalse()
        {
            // Act
            var result = await _logService.CollectLogsAsync(null!);

            // Assert
            Assert.False(result);
            _mockJsonFileManager.Verify(x => x.SaveLogsAsync(It.IsAny<string>(), It.IsAny<List<ErrorLog>>()), Times.Never);
        }

        [Fact]
        public async Task CollectLogsAsync_WithEmptyLogs_ReturnsFalse()
        {
            // Act
            var result = await _logService.CollectLogsAsync(new List<ErrorLog>());

            // Assert
            Assert.False(result);
            _mockJsonFileManager.Verify(x => x.SaveLogsAsync(It.IsAny<string>(), It.IsAny<List<ErrorLog>>()), Times.Never);
        }

        [Fact]
        public async Task CollectLogsAsync_WhenExceptionThrown_ReturnsFalse()
        {
            // Arrange
            var logs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow,
                    Source = "TestApp",
                    Message = "Test error message"
                }
            };

            _mockJsonFileManager.Setup(x => x.GetDailyLogFilePath(It.IsAny<DateTime>()))
                .Returns("logs-2024-01-01.json");
            _mockJsonFileManager.Setup(x => x.LoadLogsAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("File access error"));

            // Act
            var result = await _logService.CollectLogsAsync(logs);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetFilteredLogsAsync_WithNoFilters_ReturnsAllLogs()
        {
            // Arrange
            var mockLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    Source = "TestApp",
                    Message = "Test error 1",
                    Severity = "High",
                    Priority = "High"
                },
                new ErrorLog
                {
                    Id = "2",
                    Timestamp = DateTime.UtcNow.AddDays(-2),
                    Source = "TestApp",
                    Message = "Test error 2",
                    Severity = "Medium",
                    Priority = "Medium"
                }
            };

            _mockJsonFileManager.Setup(x => x.LoadLogsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(mockLogs);

            // Act
            var result = await _logService.GetFilteredLogsAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("1", result[0].Id); // Should be ordered by timestamp descending
        }

        [Fact]
        public async Task GetFilteredLogsAsync_WithSeverityFilter_ReturnsFilteredLogs()
        {
            // Arrange
            var mockLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    Source = "TestApp",
                    Message = "Test error 1",
                    Severity = "High",
                    Priority = "High"
                },
                new ErrorLog
                {
                    Id = "2",
                    Timestamp = DateTime.UtcNow.AddDays(-2),
                    Source = "TestApp",
                    Message = "Test error 2",
                    Severity = "Medium",
                    Priority = "Medium"
                }
            };

            _mockJsonFileManager.Setup(x => x.LoadLogsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(mockLogs);

            // Act
            var result = await _logService.GetFilteredLogsAsync(severity: "High");

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].Id);
            Assert.Equal("High", result[0].Severity);
        }

        [Fact]
        public async Task GetFilteredLogsAsync_WithPriorityFilter_ReturnsFilteredLogs()
        {
            // Arrange
            var mockLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    Source = "TestApp",
                    Message = "Test error 1",
                    Severity = "High",
                    Priority = "High"
                },
                new ErrorLog
                {
                    Id = "2",
                    Timestamp = DateTime.UtcNow.AddDays(-2),
                    Source = "TestApp",
                    Message = "Test error 2",
                    Severity = "Medium",
                    Priority = "Medium"
                }
            };

            _mockJsonFileManager.Setup(x => x.LoadLogsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(mockLogs);

            // Act
            var result = await _logService.GetFilteredLogsAsync(priority: "Medium");

            // Assert
            Assert.Single(result);
            Assert.Equal("2", result[0].Id);
            Assert.Equal("Medium", result[0].Priority);
        }

        [Fact]
        public async Task GetFilteredLogsAsync_WhenExceptionThrown_ReturnsEmptyList()
        {
            // Arrange
            _mockJsonFileManager.Setup(x => x.LoadLogsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _logService.GetFilteredLogsAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetLogStatisticsAsync_WithValidLogs_ReturnsCorrectStatistics()
        {
            // Arrange
            var mockLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    Source = "TestApp",
                    Message = "Test error 1",
                    Severity = "High",
                    Priority = "High",
                    IsAnalyzed = true
                },
                new ErrorLog
                {
                    Id = "2",
                    Timestamp = DateTime.UtcNow.AddDays(-2),
                    Source = "TestApp",
                    Message = "Test error 2",
                    Severity = "Medium",
                    Priority = "Medium",
                    IsAnalyzed = false
                },
                new ErrorLog
                {
                    Id = "3",
                    Timestamp = DateTime.UtcNow.AddDays(-3),
                    Source = "TestApp",
                    Message = "Test error 3",
                    Severity = "High",
                    Priority = "Low",
                    IsAnalyzed = true
                }
            };

            _mockJsonFileManager.Setup(x => x.LoadLogsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(mockLogs);

            // Act
            var result = await _logService.GetLogStatisticsAsync();

            // Assert
            Assert.Equal(3, result.TotalLogs);
            Assert.Equal(2, result.AnalyzedLogs);
            Assert.Equal(1, result.UnanalyzedLogs);
            Assert.Equal(2, result.SeverityBreakdown["High"]);
            Assert.Equal(1, result.SeverityBreakdown["Medium"]);
            Assert.Equal(1, result.PriorityBreakdown["High"]);
            Assert.Equal(1, result.PriorityBreakdown["Medium"]);
            Assert.Equal(1, result.PriorityBreakdown["Low"]);
        }

        [Fact]
        public async Task GetLogStatisticsAsync_WithEmptyLogs_ReturnsZeroStatistics()
        {
            // Arrange
            _mockJsonFileManager.Setup(x => x.LoadLogsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ErrorLog>());

            // Act
            var result = await _logService.GetLogStatisticsAsync();

            // Assert
            Assert.Equal(0, result.TotalLogs);
            Assert.Equal(0, result.AnalyzedLogs);
            Assert.Equal(0, result.UnanalyzedLogs);
            Assert.Empty(result.SeverityBreakdown);
            Assert.Empty(result.PriorityBreakdown);
        }

        [Fact]
        public async Task GetLogStatisticsAsync_WhenExceptionThrown_ReturnsDefaultStatistics()
        {
            // Arrange
            _mockJsonFileManager.Setup(x => x.LoadLogsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _logService.GetLogStatisticsAsync();

            // Assert
            Assert.Equal(0, result.TotalLogs);
            Assert.Equal(0, result.AnalyzedLogs);
            Assert.Equal(0, result.UnanalyzedLogs);
            Assert.Empty(result.SeverityBreakdown);
            Assert.Empty(result.PriorityBreakdown);
        }
    }
}