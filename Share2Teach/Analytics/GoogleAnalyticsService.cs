using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Share2Teach.Analytics // Updated namespace
{
    public class GoogleAnalyticsService
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string trackingId = "G-7BM1508TJZ"; // Replace with your actual Google Analytics Tracking ID
        private readonly ILogger<GoogleAnalyticsService> _logger;

        public GoogleAnalyticsService(ILogger<GoogleAnalyticsService> logger)
        {
            _logger = logger;
        }

        // Send an event to Google Analytics
        public async Task SendEventAsync(string eventCategory, string clientId, string endpointLabel)
        {
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

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("https://www.google-analytics.com/debug/collect", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Analytics event sent successfully for {eventCategory} - {endpointLabel}", eventCategory, endpointLabel);
            }
            else
            {
                _logger.LogError("Failed to send event: {statusCode}", response.StatusCode);
            }
        }
    }
}
