using Microsoft.AspNetCore.Mvc; 
using System.Threading.Tasks; 
using Microsoft.Extensions.Logging; 
using Share2Teach.Analytics; 

namespace Share2Teach.Controllers 
{
    [ApiController] // Indicates that this class is an API controller
    [Route("api/[controller]")] // Defines the route for the controller
    public class AnalyticsController : ControllerBase
    {
        // Dependency for GoogleAnalyticsService
        private readonly GoogleAnalyticsService _googleAnalyticsService;
        
        // Logger for tracking actions and errors in this controller
        private readonly ILogger<AnalyticsController> _logger;

        // Constructor that takes dependencies via dependency injection
        public AnalyticsController(GoogleAnalyticsService googleAnalyticsService, ILogger<AnalyticsController> logger)
        {
            _googleAnalyticsService = googleAnalyticsService; // Assign the service to a private field
            _logger = logger; // Assign the logger to a private field
        }

        // Endpoint to send an event to Google Analytics
        [HttpPost("send-event")] // Route for sending an event
        public async Task<IActionResult> SendEvent([FromBody] AnalyticsEventRequest request)
        {
            // Validate the incoming request
            if (request == null || string.IsNullOrWhiteSpace(request.EventCategory) || string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.EndpointLabel))
            {
                return BadRequest("Invalid request data."); // Return bad request if validation fails
            }

            try
            {
                // Send the event to Google Analytics using the service
                await _googleAnalyticsService.SendEventAsync(request.EventCategory, request.ClientId, request.EndpointLabel);

                // Return a success response
                return Ok("Event sent to Google Analytics successfully.");
            }
            catch (System.Exception ex)
            {
                // Log the error if something goes wrong
                _logger.LogError(ex, "Error while sending event to Google Analytics.");
                return StatusCode(500, "Internal server error"); // Return a 500 status code for internal errors
            }
        }
    }

    // Model for the request body when sending events
    public class AnalyticsEventRequest
    {
        public string EventCategory { get; set; } // Category of the event
        public string ClientId { get; set; } // Unique identifier for the client
        public string EndpointLabel { get; set; } // Label for the endpoint being called
    }
}
