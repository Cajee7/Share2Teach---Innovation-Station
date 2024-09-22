using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseConnection.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileDownloadController : ControllerBase
    {
        private static readonly string username = "aramsunar";
        private static readonly string password = "Jaedene12!";
        private static readonly string webdavUrl = "http://localhost:8080/remote.php/dav/files/aramsunar";

        // Download file from Nextcloud
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> Download(string fileName)
        {
            // Encode the file name to handle spaces and special characters
            var encodedFileName = Uri.EscapeDataString(fileName);
            var downloadUrl = $"{webdavUrl.TrimEnd('/')}/{encodedFileName}";

            using (var client = new HttpClient())
            {
                // Adding basic authentication headers
                var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                try
                {
                    // Log the URL for debugging purposes
                    Console.WriteLine($"Attempting to download from URL: {downloadUrl}");

                    // Make the HTTP GET request
                    var response = await client.GetAsync(downloadUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        // Read the file content as bytes
                        var fileBytes = await response.Content.ReadAsByteArrayAsync();

                        // Get content type from response if available, default to 'application/octet-stream'
                        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

                        // Return the file as a downloadable response
                        return File(fileBytes, contentType, fileName);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Specific handling for 404 Not Found
                        return NotFound(new { message = $"File '{fileName}' not found on the server." });
                    }
                    else
                    {
                        // Log the failed status code and return it
                        Console.WriteLine($"Failed to download. Status Code: {response.StatusCode}");
                        return StatusCode((int)response.StatusCode, new { message = $"Download failed: {response.StatusCode}" });
                    }
                }
                catch (HttpRequestException ex)
                {
                    // Network-related exception
                    return StatusCode(500, new { message = $"Network error during download: {ex.Message}" });
                }
                catch (Exception ex)
                {
                    // Log general exceptions
                    Console.WriteLine($"Exception during download: {ex}");
                    return StatusCode(500, new { message = $"Exception during download: {ex.Message}" });
                }
            }
        }
    }
}

/*class FileDownload
{
    static async Task Main(string[] args)
    {   
        // WebDAV URL to the file you want to download
        string webdavUrl = "http://localhost:8080/remote.php/dav/files/aramsunar/uploaded-file.txt";
        string username = Environment.GetEnvironmentVariable("WEBDAV_USERNAME") ?? "aramsunar";
        string password = Environment.GetEnvironmentVariable("WEBDAV_PASSWORD") ?? "Jaedene12!";
        
        // Local path where the file will be saved
        string localFilePath = @"C:\path\to\save\file.txt";

        using (HttpClient client = new HttpClient())
        {
            // Basic Authentication
            var byteArray = new System.Text.ASCIIEncoding().GetBytes($"{username}:{password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            try
            {
                // Download file
                HttpResponseMessage response = await client.GetAsync(webdavUrl);

                if (response.IsSuccessStatusCode)
                {
                    byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(localFilePath, fileBytes);
                    Console.WriteLine("File downloaded successfully.");
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
    }
}
*/


