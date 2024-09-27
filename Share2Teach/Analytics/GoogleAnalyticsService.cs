using System.Net.Http; 
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; 
using System.Collections.Generic; 

namespace Share2Teach.Analytics 
{
    public class GoogleAnalyticsService
    {
        // Static HttpClient instance for sending HTTP requests
        private static readonly HttpClient client = new HttpClient();
        
        // Google Analytics tracking ID 
        private readonly string trackingId = "G-7BM1508TJZ"; 
        
        // Logger instance for logging messages and errors
        private readonly ILogger<GoogleAnalyticsService> _logger;

        // Constructor that takes a logger as a dependency
        public GoogleAnalyticsService(ILogger<GoogleAnalyticsService> logger)
        {
            _logger = logger; // Assign the logger to the private field
        }

        // Method to send an event to Google Analytics
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

