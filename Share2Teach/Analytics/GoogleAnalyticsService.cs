using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Share2Teach.Analytics
{
    /// <summary>
    /// Service for sending events to Google Analytics.
    /// </summary>
    public class GoogleAnalyticsService
    {
        // Static HttpClient instance for sending HTTP requests
        private static readonly HttpClient client = new HttpClient();
        
        // Google Analytics tracking ID 
        private readonly string trackingId = "G-7BM1508TJZ"; 
        
        // Logger instance for logging messages and errors
        private readonly ILogger<GoogleAnalyticsService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleAnalyticsService"/> class.
        /// </summary>
        /// <param name="logger">Logger to log information and errors.</param>
        public GoogleAnalyticsService(ILogger<GoogleAnalyticsService> logger)
        {
            _logger = logger; // Assign the logger to the private field
        }

        /// <summary>
        /// Sends an event to Google Analytics with the specified parameters.
        /// </summary>
        /// <param name="eventCategory">Category of the event (e.g., API request).</param>
        /// <param name="clientId">Unique client identifier (based on IP and User-Agent).</param>
        /// <param name="endpointLabel">Label describing the endpoint being accessed.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SendEventAsync(string eventCategory, string clientId, string endpointLabel)
        {
            // Prepare the data to be sent in the request
            var values = new Dictionary<string, string>
            {
                { "v", "1" }, // Protocol version
                { "tid", trackingId }, // Tracking ID
                { "cid", clientId }, // Client ID
                { "t", "event" }, // Hit type (event)
                { "ec", eventCategory }, // Event Category
                { "ea", "endpoint_call" }, // Event Action
                { "el", endpointLabel }, // Event Label
                { "ev", "1" }, // Event value
                { "ni", "1" }, // Non-interaction hit
                { "debug", "true" } // Enable debug mode
            };

            // Create the content for the POST request
            var content = new FormUrlEncodedContent(values);

            // Send the POST request to Google Analytics
            var response = await client.PostAsync("https://www.google-analytics.com/debug/collect", content);

            // Check if the response was successful
            if (response.IsSuccessStatusCode)
            {
                // Log success message if the event was sent successfully
                _logger.LogInformation("Analytics event sent successfully for {eventCategory} - {endpointLabel}", eventCategory, endpointLabel);
            }
            else
            {
                // Log an error message if the event fails to send
                _logger.LogError("Failed to send event: {statusCode}", response.StatusCode);
            }
        }
    }
}
