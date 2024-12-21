using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace Share2Teach.LoadTests
{
    public class FAQLoadTester
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl;
        private readonly Dictionary<string, List<RequestResult>> _endpointResults;
        private readonly string _logDirectory;
        private readonly string _adminToken; // You'll need to provide this

        public FAQLoadTester(string baseUrl, string adminToken)
        {
            _client = new HttpClient();
            _baseUrl = baseUrl.TrimEnd('/');
            _adminToken = adminToken;
            _endpointResults = new Dictionary<string, List<RequestResult>>();
            _logDirectory = Path.Combine("Logs", "LoadTests", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            Directory.CreateDirectory(_logDirectory);
        }

        public async Task RunLoadTest(int numberOfUsers, int durationSeconds)
        {
            var endpoints = new List<EndpointTest>
            {
                // Public endpoints
                new EndpointTest
                {
                    Name = "GetAllFAQs",
                    Method = HttpMethod.Get,
                    Path = "/api/FAQ/list",
                    RequiresAuth = false
                },
                new EndpointTest
                {
                    Name = "GetFAQById",
                    Method = HttpMethod.Get,
                    Path = "/api/FAQ/{id}",
                    RequiresAuth = false,
                    PathParamGenerator = GenerateRandomObjectId
                },
                // Admin endpoints
                new EndpointTest
                {
                    Name = "AddFAQ",
                    Method = HttpMethod.Post,
                    Path = "/api/FAQ/add",
                    RequiresAuth = true,
                    PayloadGenerator = GenerateAddFAQPayload
                },
                new EndpointTest
                {
                    Name = "UpdateFAQ",
                    Method = HttpMethod.Put,
                    Path = "/api/FAQ/update",
                    RequiresAuth = true,
                    PayloadGenerator = GenerateAddFAQPayload,
                    QueryParamGenerator = () => $"id={GenerateRandomObjectId()}"
                },
                new EndpointTest
                {
                    Name = "DeleteFAQ",
                    Method = HttpMethod.Delete,
                    Path = "/api/FAQ/delete",
                    RequiresAuth = true,
                    QueryParamGenerator = () => $"id={GenerateRandomObjectId()}"
                }
            };

            foreach (var endpoint in endpoints)
            {
                _endpointResults[endpoint.Name] = new List<RequestResult>();
            }

            var tasks = new List<Task>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(durationSeconds));

            Console.WriteLine($"Starting FAQ API load test with {numberOfUsers} users for {durationSeconds} seconds");

            for (int i = 0; i < numberOfUsers; i++)
            {
                foreach (var endpoint in endpoints)
                {
                    tasks.Add(SimulateUserForEndpoint(endpoint, cts.Token));
                }
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Load test completed.");
            }

            GenerateReport();
        }

        private async Task SimulateUserForEndpoint(EndpointTest endpoint, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    HttpResponseMessage response;
                    string path = endpoint.Path;
                    
                    // Handle path parameters
                    if (endpoint.PathParamGenerator != null)
                    {
                        path = path.Replace("{id}", endpoint.PathParamGenerator());
                    }

                    // Handle query parameters
                    if (endpoint.QueryParamGenerator != null)
                    {
                        path = $"{path}?{endpoint.QueryParamGenerator()}";
                    }

                    var request = new HttpRequestMessage(endpoint.Method, $"{_baseUrl}{path}");

                    // Add authorization if required
                    if (endpoint.RequiresAuth)
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
                    }

                    // Add content for POST/PUT requests
                    if (endpoint.PayloadGenerator != null && (endpoint.Method == HttpMethod.Post || endpoint.Method == HttpMethod.Put))
                    {
                        request.Content = endpoint.PayloadGenerator();
                    }

                    response = await _client.SendAsync(request, ct);

                    lock (_endpointResults)
                    {
                        _endpointResults[endpoint.Name].Add(new RequestResult
                        {
                            Duration = sw.ElapsedMilliseconds,
                            IsSuccess = response.IsSuccessStatusCode,
                            StatusCode = (int)response.StatusCode,
                            Endpoint = endpoint.Name
                        });
                    }
                }
                catch (Exception ex)
                {
                    lock (_endpointResults)
                    {
                        _endpointResults[endpoint.Name].Add(new RequestResult
                        {
                            Duration = sw.ElapsedMilliseconds,
                            IsSuccess = false,
                            Error = ex.Message,
                            Endpoint = endpoint.Name
                        });
                    }
                }

                await Task.Delay(Random.Shared.Next(100, 500), ct);
            }
        }

        private string GenerateRandomObjectId()
        {
            return Convert.ToHexString(Guid.NewGuid().ToByteArray()).Substring(0, 24).ToLower();
        }

        private HttpContent GenerateAddFAQPayload()
        {
            var faq = new
            {
                Question = $"Test Question {DateTime.Now.Ticks}",
                Answer = $"Test Answer {DateTime.Now.Ticks}"
            };

            return new StringContent(
                JsonSerializer.Serialize(faq),
                Encoding.UTF8,
                "application/json");
        }

        private void GenerateReport()
        {
            var reportBuilder = new StringBuilder();
            reportBuilder.AppendLine("Share2Teach FAQ API Load Test Report");
            reportBuilder.AppendLine("====================================");
            reportBuilder.AppendLine($"Test Time: {DateTime.Now}");
            reportBuilder.AppendLine();

            foreach (var endpoint in _endpointResults)
            {
                var results = endpoint.Value;
                if (results.Count == 0) continue;

                reportBuilder.AppendLine($"Endpoint: {endpoint.Key}");
                reportBuilder.AppendLine($"Total Requests: {results.Count}");
                reportBuilder.AppendLine($"Success Rate: {(results.Count(r => r.IsSuccess) * 100.0 / results.Count):F2}%");
                reportBuilder.AppendLine($"Average Response Time: {results.Average(r => r.Duration):F2}ms");
                reportBuilder.AppendLine($"Min Response Time: {results.Min(r => r.Duration)}ms");
                reportBuilder.AppendLine($"Max Response Time: {results.Max(r => r.Duration)}ms");
                reportBuilder.AppendLine($"95th Percentile: {Calculate95thPercentile(results):F2}ms");
                
                // Status code distribution
                var statusCodes = results
                    .GroupBy(r => r.StatusCode)
                    .OrderBy(g => g.Key);

                reportBuilder.AppendLine("\nStatus Code Distribution:");
                foreach (var statusGroup in statusCodes)
                {
                    var percentage = (statusGroup.Count() * 100.0 / results.Count);
                    reportBuilder.AppendLine($"  HTTP {statusGroup.Key}: {statusGroup.Count()} ({percentage:F2}%)");
                }
                
                reportBuilder.AppendLine("\n");

                // Generate CSV for this endpoint
                var csvPath = Path.Combine(_logDirectory, $"{endpoint.Key}_results.csv");
                File.WriteAllText(csvPath, GenerateCsv(results));
            }

            var reportPath = Path.Combine(_logDirectory, "LoadTestReport.txt");
            File.WriteAllText(reportPath, reportBuilder.ToString());

            Console.WriteLine($"Report generated at: {_logDirectory}");
        }

        private double Calculate95thPercentile(List<RequestResult> results)
        {
            var sortedDurations = results.Select(r => r.Duration).OrderBy(d => d).ToList();
            var index = (int)Math.Ceiling(sortedDurations.Count * 0.95) - 1;
            return sortedDurations[index];
        }

        private string GenerateCsv(List<RequestResult> results)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,Endpoint,Duration,IsSuccess,StatusCode,Error");
            
            foreach (var result in results)
            {
                csv.AppendLine($"{result.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                             $"{result.Endpoint}," +
                             $"{result.Duration}," +
                             $"{result.IsSuccess}," +
                             $"{result.StatusCode}," +
                             $"\"{result.Error?.Replace("\"", "\"\"")}\"");
            }

            return csv.ToString();
        }
    }

    public class EndpointTest
    {
        public string Name { get; set; }
        public HttpMethod Method { get; set; }
        public string Path { get; set; }
        public bool RequiresAuth { get; set; }
        public Func<HttpContent> PayloadGenerator { get; set; }
        public Func<string> QueryParamGenerator { get; set; }
        public Func<string> PathParamGenerator { get; set; }
    }

    public class RequestResult
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Endpoint { get; set; }
        public long Duration { get; set; }
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string Error { get; set; }
    }
}