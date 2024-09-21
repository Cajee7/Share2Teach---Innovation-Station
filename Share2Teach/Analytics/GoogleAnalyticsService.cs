using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DatabaseConnection.Services  // Replace with your project's namespace
{
    public class GoogleAnalyticsService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string MeasurementId = "G-7BM1508TJZ";  // Your Google Analytics Measurement ID
        private const string ApiSecret = "3A6rQ8WiRDCd-jkLheOnvg";  // Replace with your API secret

        public async Task SendEventAsync(string userinteraction, string userId = null)
        {
            var requestUrl = $"https://www.google-analytics.com/mp/collect?measurement_id={MeasurementId}&api_secret={ApiSecret}";

            // Define eventData before it's used
            var eventData = new
            {
                client_id = userId ?? Guid.NewGuid().ToString(), // A unique identifier for the user
                events = new[]
                {
                    new
                    {
                        name = userinteraction,
                        event_params = new // Change from 'params' to 'event_params' or any other valid name
                        {
                            engagement_time_msec = 100
                            // Add more event-specific parameters here if needed
                        }
                    }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(eventData), System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(requestUrl, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Event sent successfully");
            }
            else
            {
                Console.WriteLine($"Error sending event: {response.StatusCode}");
            }
        }
    }
}
