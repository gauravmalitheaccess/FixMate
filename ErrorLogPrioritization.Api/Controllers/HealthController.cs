using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Swashbuckle.AspNetCore.Annotations;
using System.Diagnostics;
using System.Net;

namespace ErrorLogPrioritization.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Health Checks", "System health and status monitoring endpoints")]
    [Produces("application/json")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger)
        {
            _healthCheckService = healthCheckService;
            _logger = logger;
        }

        /// <summary>
        /// Get comprehensive system health status
        /// </summary>
        /// <returns>Detailed health check results</returns>
        /// <remarks>
        /// Sample response:
        /// 
        ///     {
        ///         "status": "Healthy",
        ///         "totalDuration": "00:00:01.234",
        ///         "entries": {
        ///             "filesystem": {
        ///                 "status": "Healthy",
        ///                 "duration": "00:00:00.123",
        ///                 "description": "File system is accessible"
        ///             },
        ///             "copilot-studio": {
        ///                 "status": "Healthy",
        ///                 "duration": "00:00:00.456",
        ///                 "description": "Copilot Studio API is responding"
        ///             }
        ///         }
        ///     }
        /// 
        /// </remarks>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get system health status",
            Description = "Returns comprehensive health check results for all system components including file system, external APIs, and background services.",
            OperationId = "GetHealth",
            Tags = new[] { "System Health" }
        )]
        [SwaggerResponse(200, "System is healthy", typeof(object))]
        [SwaggerResponse(503, "System is unhealthy", typeof(object))]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                var healthReport = await _healthCheckService.CheckHealthAsync();
                
                var response = new
                {
                    status = healthReport.Status.ToString(),
                    totalDuration = healthReport.TotalDuration.ToString(),
                    entries = healthReport.Entries.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new
                        {
                            status = kvp.Value.Status.ToString(),
                            duration = kvp.Value.Duration.ToString(),
                            description = kvp.Value.Description,
                            data = kvp.Value.Data
                        })
                };

                var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 503;
                
                _logger.LogInformation("Health check completed with status: {Status}", healthReport.Status);
                
                return StatusCode(statusCode, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during health check");
                return StatusCode(503, new { 
                    status = "Unhealthy", 
                    error = "Health check failed",
                    message = ex.Message 
                });
            }
        }

        /// <summary>
        /// Get basic system status for load balancer health checks
        /// </summary>
        /// <returns>Simple OK response if system is healthy</returns>
        [HttpGet("ready")]
        [SwaggerOperation(
            Summary = "Readiness probe",
            Description = "Simple endpoint for load balancer readiness checks. Returns 200 OK if the system is ready to serve requests.",
            OperationId = "GetReadiness",
            Tags = new[] { "System Health" }
        )]
        [SwaggerResponse(200, "System is ready")]
        [SwaggerResponse(503, "System is not ready")]
        public async Task<IActionResult> GetReadiness()
        {
            try
            {
                var healthReport = await _healthCheckService.CheckHealthAsync();
                
                if (healthReport.Status == HealthStatus.Healthy)
                {
                    return Ok(new { status = "Ready" });
                }
                else
                {
                    return StatusCode(503, new { status = "Not Ready" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during readiness check");
                return StatusCode(503, new { status = "Not Ready", error = ex.Message });
            }
        }

        /// <summary>
        /// Get basic liveness status for container orchestration
        /// </summary>
        /// <returns>Simple OK response if application is alive</returns>
        [HttpGet("live")]
        [SwaggerOperation(
            Summary = "Liveness probe",
            Description = "Simple endpoint for container liveness checks. Returns 200 OK if the application is running.",
            OperationId = "GetLiveness",
            Tags = new[] { "System Health" }
        )]
        [SwaggerResponse(200, "Application is alive")]
        public IActionResult GetLiveness()
        {
            return Ok(new { status = "Alive", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Get system information and version details
        /// </summary>
        /// <returns>System information including version, environment, and uptime</returns>
        [HttpGet("info")]
        [SwaggerOperation(
            Summary = "Get system information",
            Description = "Returns system information including application version, environment, and runtime details.",
            OperationId = "GetSystemInfo",
            Tags = new[] { "System Information" }
        )]
        [SwaggerResponse(200, "System information retrieved successfully", typeof(object))]
        public IActionResult GetSystemInfo()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version?.ToString() ?? "Unknown";
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
                
                var systemInfo = new
                {
                    application = new
                    {
                        name = "Error Log Prioritization API",
                        version = version,
                        environment = environment,
                        framework = Environment.Version.ToString(),
                        startTime = Process.GetCurrentProcess().StartTime,
                        uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime
                    },
                    system = new
                    {
                        machineName = Environment.MachineName,
                        osVersion = Environment.OSVersion.ToString(),
                        processorCount = Environment.ProcessorCount,
                        workingSet = Environment.WorkingSet,
                        gcMemory = GC.GetTotalMemory(false)
                    },
                    timestamp = DateTime.UtcNow
                };

                return Ok(systemInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving system information");
                return StatusCode(500, new { error = "Failed to retrieve system information" });
            }
        }
    }
}