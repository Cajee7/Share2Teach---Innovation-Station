using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

namespace Share2Teach.Analytics
{
    /// <summary>
    /// Middleware for sending requests and events to Google Analytics.
    /// </summary>
    public class GoogleAnalyticsMiddleware
    {
        private readonly RequestDelegate _next; // Delegate to call the next middleware in the pipeline
        private readonly GoogleAnalyticsService _googleAnalyticsService; // Service for sending events to Google Analytics
        private readonly ILogger<GoogleAnalyticsMiddleware> _logger; // Logger for tracking actions and errors

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleAnalyticsMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the request pipeline.</param>
        /// <param name="googleAnalyticsService">Service to send events to Google Analytics.</param>
        /// <param name="logger">Logger to log request details and errors.</param>
        public GoogleAnalyticsMiddleware(RequestDelegate next, GoogleAnalyticsService googleAnalyticsService, ILogger<GoogleAnalyticsMiddleware> logger)
        {
            _next = next; // Assign the next middleware
            _googleAnalyticsService = googleAnalyticsService; // Assign the Google Analytics service
            _logger = logger; // Assign the logger
        }

        /// <summary>
        /// Invokes the middleware to process the HTTP context and send events to Google Analytics.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Invoke(HttpContext context)
        {
            // Log the request details before proceeding to the next middleware
            var eventCategory = "API Request";
            var clientId = $"{context.Connection.RemoteIpAddress}-{context.Request.Headers["User-Agent"]}"; // Create a unique identifier using IP and User-Agent
            var endpointLabel = $"{context.Request.Method} {context.Request.Path}";

            _logger.LogInformation("Handling request for {Endpoint} with ClientId: {ClientId}", endpointLabel, clientId);

            try
            {
                // Call the next middleware in the pipeline (this will process the request and generate the response)
                await _next(context);

                // Send the event to Google Analytics after the request is processed
                await _googleAnalyticsService.SendEventAsync(eventCategory, clientId, endpointLabel);

                // Log success after sending the event
                _logger.LogInformation("Google Analytics event sent for {Endpoint} with status {StatusCode}", endpointLabel, context.Response.StatusCode);
            }
            catch (Exception ex)
            {
                // Handle and log any errors
                _logger.LogError("Failed to process request for {Endpoint}: {Exception}", endpointLabel, ex.Message);
                throw; // Re-throw the exception to allow the request pipeline to handle it
            }
        }
    }
}
