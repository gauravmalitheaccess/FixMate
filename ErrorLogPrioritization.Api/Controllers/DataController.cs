using ErrorLogPrioritization.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ErrorLogPrioritization.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Data Management", "Operations for managing test data and system initialization")]
    public class DataController : ControllerBase
    {
        private readonly ILogger<DataController> _logger;

        public DataController( ILogger<DataController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Seed the system with dummy error log data for testing
        /// </summary>
        /// <returns>Success status</returns>
        /// <remarks>
        /// This endpoint generates sample error logs for the last 7 days to help with testing and demonstration.
        /// The dummy data includes various error types, severities, and priorities.
        /// 
        /// Sample response:
        /// 
        ///     {
        ///         "message": "Successfully seeded 245 dummy logs",
        ///         "logsGenerated": 245,
        ///         "daysOfData": 7
        ///     }
        /// 
        /// </remarks>
        

        /// <summary>
        /// Check if the system has existing log data
        /// </summary>
        /// <returns>Data existence status</returns>
        /// <remarks>
        /// This endpoint checks if the system already contains log data from the last 7 days.
        /// Useful for determining whether dummy data seeding is needed.
        /// 
        /// Sample response:
        /// 
        ///     {
        ///         "hasData": true,
        ///         "message": "System contains existing log data"
        ///     }
        /// 
        /// </remarks>
       
    }
}