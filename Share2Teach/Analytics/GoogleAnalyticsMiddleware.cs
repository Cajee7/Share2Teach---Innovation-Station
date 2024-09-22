using Microsoft.AspNetCore.Http; 
using System.Threading.Tasks; 
using Microsoft.Extensions.Logging; 

namespace Share2Teach.Analytics 
{
    public class GoogleAnalyticsMiddleware
    {
        private readonly RequestDelegate _next; // Delegate to call the next middleware in the pipeline
        private readonly GoogleAnalyticsService _googleAnalyticsService; // Service for sending events to Google Analytics
        private readonly ILogger<GoogleAnalyticsMiddleware> _logger; // Logger for tracking actions and errors

        // Constructor that takes dependencies via dependency injection
        public GoogleAnalyticsMiddleware(RequestDelegate next, GoogleAnalyticsService googleAnalyticsService, ILogger<GoogleAnalyticsMiddleware> logger)
        {
            _next = next; // Assign the next middleware
            _googleAnalyticsService = googleAnalyticsService; // Assign the Google Analytics service
            _logger = logger; // Assign the logger
        }

        // Method to handle the incoming HTTP context
        public async Task Invoke(HttpContext context)
        {
            // Call the next middleware in the pipeline first
            await _next(context);

            // Logic for sending event to Google Analytics
            var eventCategory = "API Request"; // Set the event category for tracking
            var clientId = context.Request.Headers["User-Agent"].ToString(); // Get a unique identifier from the request headers (User-Agent)
            var endpointLabel = $"{context.Request.Method} {context.Request.Path}"; // Create a label for the endpoint being called

            // Send the event to Google Analytics
            try
            {
                // Call the service method to send the event
                await _googleAnalyticsService.SendEventAsync(eventCategory, clientId, endpointLabel);
                _logger.LogInformation("Google Analytics event sent for {Endpoint}", endpointLabel); // Log success
            }
            catch (Exception ex)
            {
                // Log an error if the event fails to send
                _logger.LogError("Failed to send Google Analytics event: {Exception}", ex.Message);
            }
        }
    }
}
