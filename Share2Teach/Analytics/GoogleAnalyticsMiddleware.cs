using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Share2Teach.Analytics // Updated namespace
{
    public class GoogleAnalyticsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly GoogleAnalyticsService _googleAnalyticsService;
        private readonly ILogger<GoogleAnalyticsMiddleware> _logger;

        public GoogleAnalyticsMiddleware(RequestDelegate next, GoogleAnalyticsService googleAnalyticsService, ILogger<GoogleAnalyticsMiddleware> logger)
        {
            _next = next;
            _googleAnalyticsService = googleAnalyticsService;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            // Call the next middleware in the pipeline first
            await _next(context);

            // Logic for sending event to Google Analytics
            var eventCategory = "API Request";  // Set appropriate event category
            var clientId = context.Request.Headers["User-Agent"].ToString();  // Or any other unique identifier
            var endpointLabel = $"{context.Request.Method} {context.Request.Path}";

            // Send the event to Google Analytics
            try
            {
                await _googleAnalyticsService.SendEventAsync(eventCategory, clientId, endpointLabel);
                _logger.LogInformation("Google Analytics event sent for {Endpoint}", endpointLabel);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send Google Analytics event: {Exception}", ex.Message);
            }
        }
    }
}
