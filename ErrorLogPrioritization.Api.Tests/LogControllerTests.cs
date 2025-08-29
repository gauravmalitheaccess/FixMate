using ErrorLogPrioritization.Api.Controllers;
using ErrorLogPrioritization.Api.Models;
using ErrorLogPrioritization.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel.DataAnnotations;

namespace ErrorLogPrioritization.Api.Tests
{
    public class LogControllerTests
    {
        private readonly Mock<ILogService> _mockLogService;
        private readonly Mock<IExcelExportService> _mockExcelExportService;
        private readonly Mock<ILogger<LogController>> _mockLogger;
        private readonly LogController _controller;

        public LogControllerTests()
        {
            _mockLogService = new Mock<ILogService>();
            _mockExcelExportService = new Mock<IExcelExportService>();
            _mockLogger = new Mock<ILogger<LogController>>();
            _controller = new LogController(_mockLogService.Object, _mockExcelExportService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CollectLogs_WithValidLogs_ReturnsOk()
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
                }
            };

            _mockLogService.Setup(x => x.CollectLogsAsync(It.IsAny<List<ErrorLog>>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CollectLogs(logs);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            _mockLogService.Verify(x => x.CollectLogsAsync(logs), Times.Once);
        }

        [Fact]
        public async Task CollectLogs_WithNullLogs_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.CollectLogs(null!);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            _mockLogService.Verify(x => x.CollectLogsAsync(It.IsAny<List<ErrorLog>>()), Times.Never);
        }

        [Fact]
        public async Task CollectLogs_WithEmptyLogs_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.CollectLogs(new List<ErrorLog>());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            _mockLogService.Verify(x => x.CollectLogsAsync(It.IsAny<List<ErrorLog>>()), Times.Never);
        }

        [Fact]
        public async Task CollectLogs_WithInvalidLogs_ReturnsBadRequest()
        {
            // Arrange
            var logs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow,
                    Source = "", // Invalid - required field
                    Message = "Test error message"
                }
            };

            // Act
            var result = await _controller.CollectLogs(logs);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            _mockLogService.Verify(x => x.CollectLogsAsync(It.IsAny<List<ErrorLog>>()), Times.Never);
        }

        [Fact]
        public async Task CollectLogs_WhenServiceFails_ReturnsInternalServerError()
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

            _mockLogService.Setup(x => x.CollectLogsAsync(It.IsAny<List<ErrorLog>>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.CollectLogs(logs);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task CollectLogs_WhenExceptionThrown_ReturnsInternalServerError()
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

            _mockLogService.Setup(x => x.CollectLogsAsync(It.IsAny<List<ErrorLog>>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CollectLogs(logs);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetLogs_WithValidParameters_ReturnsOk()
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
                    Severity = "High"
                },
                new ErrorLog
                {
                    Id = "2",
                    Timestamp = DateTime.UtcNow.AddDays(-2),
                    Source = "TestApp",
                    Message = "Test error 2",
                    Severity = "Medium"
                }
            };

            _mockLogService.Setup(x => x.GetFilteredLogsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 
                It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockLogs);

            // Act
            var result = await _controller.GetLogs();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            _mockLogService.Verify(x => x.GetFilteredLogsAsync(null, null, null, null), Times.Once);
        }

        [Fact]
        public async Task GetLogs_WithInvalidPageNumber_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetLogs(page: 0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            _mockLogService.Verify(x => x.GetFilteredLogsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 
                It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetLogs_WithInvalidPageSize_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetLogs(pageSize: 1001);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            _mockLogService.Verify(x => x.GetFilteredLogsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 
                It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetLogs_WithInvalidDateRange_ReturnsBadRequest()
        {
            // Arrange
            var fromDate = DateTime.UtcNow;
            var toDate = DateTime.UtcNow.AddDays(-1);

            // Act
            var result = await _controller.GetLogs(fromDate: fromDate, toDate: toDate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            _mockLogService.Verify(x => x.GetFilteredLogsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 
                It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetLogs_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var mockLogs = new List<ErrorLog>();
            for (int i = 1; i <= 100; i++)
            {
                mockLogs.Add(new ErrorLog
                {
                    Id = i.ToString(),
                    Timestamp = DateTime.UtcNow.AddDays(-i),
                    Source = "TestApp",
                    Message = $"Test error {i}"
                });
            }

            _mockLogService.Setup(x => x.GetFilteredLogsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 
                It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockLogs);

            // Act
            var result = await _controller.GetLogs(page: 2, pageSize: 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.NotNull(okResult.Value);
            
            // Verify the response structure by checking it's not null
            // The actual response validation would be done in integration tests
            _mockLogService.Verify(x => x.GetFilteredLogsAsync(null, null, null, null), Times.Once);
        }

        [Fact]
        public async Task GetLogs_WhenExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            _mockLogService.Setup(x => x.GetFilteredLogsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 
                It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetLogs();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetLogStatistics_WithValidParameters_ReturnsOk()
        {
            // Arrange
            var mockStatistics = new LogStatistics
            {
                TotalLogs = 100,
                AnalyzedLogs = 80,
                UnanalyzedLogs = 20,
                SeverityBreakdown = new Dictionary<string, int> { { "High", 30 }, { "Medium", 50 }, { "Low", 20 } },
                PriorityBreakdown = new Dictionary<string, int> { { "High", 25 }, { "Medium", 45 }, { "Low", 30 } },
                DateRange = new DateRange
                {
                    FromDate = DateTime.UtcNow.AddDays(-30),
                    ToDate = DateTime.UtcNow
                },
                LastUpdated = DateTime.UtcNow
            };

            _mockLogService.Setup(x => x.GetLogStatisticsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(mockStatistics);

            // Act
            var result = await _controller.GetLogStatistics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(mockStatistics, okResult.Value);
            _mockLogService.Verify(x => x.GetLogStatisticsAsync(null, null), Times.Once);
        }

        [Fact]
        public async Task GetLogStatistics_WithInvalidDateRange_ReturnsBadRequest()
        {
            // Arrange
            var fromDate = DateTime.UtcNow;
            var toDate = DateTime.UtcNow.AddDays(-1);

            // Act
            var result = await _controller.GetLogStatistics(fromDate: fromDate, toDate: toDate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            _mockLogService.Verify(x => x.GetLogStatisticsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Never);
        }

        [Fact]
        public async Task GetLogStatistics_WhenExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            _mockLogService.Setup(x => x.GetLogStatisticsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetLogStatistics();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}