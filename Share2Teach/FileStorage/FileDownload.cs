using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseConnection.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DownloadFile : ControllerBase
    {
        private static readonly string username = "aramsunar";
        private static readonly string password = "Jaedene12!";
        private static readonly string webdavUrl = "http://localhost:8080/remote.php/dav/files/aramsunar/";

        // Download file from Nextcloud
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> Download(string fileName)
        {
            var downloadUrl = $"{webdavUrl}/{fileName}";

            using (var client = new HttpClient())
            {
                var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                try
                {
                    var response = await client.GetAsync(downloadUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var fileBytes = await response.Content.ReadAsByteArrayAsync();
                        return File(fileBytes, "application/octet-stream", fileName);
                    }
                    else
                    {
                        return StatusCode((int)response.StatusCode, new { message = $"Download failed: {response.StatusCode}" });
                    }
                }
                catch (Exception ex)
                {
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


