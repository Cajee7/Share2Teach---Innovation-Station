using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Share2Teach.Analytics;

namespace Share2Teach.Controllers // Adjust this if your controllers have a different namespace
{
    
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly GoogleAnalyticsService _googleAnalyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(GoogleAnalyticsService googleAnalyticsService, ILogger<AnalyticsController> logger)
    {
        _googleAnalyticsService = googleAnalyticsService;
        _logger = logger;
    }

    // Endpoint to send event to Google Analytics
    [HttpPost("send-event")]
    public async Task<IActionResult> SendEvent([FromBody] AnalyticsEventRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.EventCategory) || string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.EndpointLabel))
        {
            return BadRequest("Invalid request data.");
        }

        try
        {
            // Send event to Google Analytics
            await _googleAnalyticsService.SendEventAsync(request.EventCategory, request.ClientId, request.EndpointLabel);

            // Return success response
            return Ok("Event sent to Google Analytics successfully.");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error while sending event to Google Analytics.");
            return StatusCode(500, "Internal server error");
        }
    }
}

// Model for the request
public class AnalyticsEventRequest
{
    public string EventCategory { get; set; }
    public string ClientId { get; set; }
    public string EndpointLabel { get; set; }
}

}