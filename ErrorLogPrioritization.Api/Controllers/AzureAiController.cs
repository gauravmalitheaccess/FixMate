using Azure.Core;
using Azure.Identity;
using ErrorLogPrioritization.Api.Services;
using ErrorLogPrioritization.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Newtonsoft.Json;

namespace ErrorLogPrioritization.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AzureAiController : ControllerBase
    {
        private readonly ILogService _logService;
        private readonly ILogger<AzureAiController> _logger;

        public AzureAiController(ILogService logService, ILogger<AzureAiController> logger)
        {
            _logService = logService;
            _logger = logger;
        }

        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var response = await AzureAiFoundryClient.ProcessFormAssistanceRequest(request);

                var result = JsonConvert.DeserializeObject<ErrorLogModel>(response);

                //var errorLog = new ErrorLog
                //{
                //    Id = Guid.NewGuid().ToString(),
                //    Timestamp = DateTime.UtcNow,
                //    Source = "AzureAi.SendMessage",
                //    Message = $"AI Analysis Request: {request.Message}",
                //    StackTrace = response ?? "No response from AI service",
                //    Severity =response.,
                //    Priority = "Low",
                //    AiReasoning = "AI-generated response from send-message endpoint",
                //    PotentialFix = response ?? "No AI suggestion available",
                //    AnalyzedAt = DateTime.UtcNow,
                //    IsAnalyzed = true
                //};

                result.ErrorLog.AnalyzedAt = DateTime.UtcNow;
                result.ErrorLog.Timestamp = DateTime.UtcNow;
                result.ErrorLog.AdoBug = result.AdoBug;

                await _logService.CollectLogsAsync(new List<ErrorLog> { result.ErrorLog });
                
                _logger.LogInformation("Stored AI response as ErrorLog entry with ID: {LogId}", result.ErrorLog.Id);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI message request");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class SendMessageRequest
    {
        public string Message { get; set; }
    }
}
