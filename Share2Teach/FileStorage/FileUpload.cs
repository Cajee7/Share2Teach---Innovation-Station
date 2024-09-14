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
    public class UploadFile : ControllerBase
    {
        private static readonly string username = "aramsunar";
        private static readonly string password = "Jaedene12!";
        private static readonly string webdavUrl = "http://localhost:8080/remote.php/dav/files/aramsunar/";

        // Upload file to Nextcloud
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] FileUploadDto uploadDto)
        {
            if (uploadDto.File == null || uploadDto.File.Length == 0)
                return BadRequest(new { message = "No file provided." });

            var uploadUrl = $"{webdavUrl}/{uploadDto.File.FileName}";

            using (var client = new HttpClient())
            {
                var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                using (var content = new StreamContent(uploadDto.File.OpenReadStream()))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    try
                    {
                        var response = await client.PutAsync(uploadUrl, content);

                        if (response.IsSuccessStatusCode)
                        {
                            return Ok(new { message = "File uploaded successfully." });
                        }
                        else
                        {
                            return StatusCode((int)response.StatusCode, new { message = $"Upload failed: {response.StatusCode}" });
                        }
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, new { message = $"Exception during upload: {ex.Message}" });
                    }
                }
            }
        }
    }
}

/*class FileUpload
{
    static async Task Main(string[] args)
    {
        // WebDAV URL to the folder where you want to upload the file
        string webdavUrl = "http://localhost:8080/remote.php/dav/files/aramsunar/";
        string username = Environment.GetEnvironmentVariable("WEBDAV_USERNAME") ?? "aramsunar";
        string password = Environment.GetEnvironmentVariable("WEBDAV_PASSWORD") ?? "Jaedene12!";

        // The file you want to upload
        string localFilePath = @"C:\path\to\your\file.txt";
        string remoteFileName = "uploaded-file.txt"; // The name you want for the uploaded file

        using (HttpClient client = new HttpClient())
        {
            // Basic Authentication
            var byteArray = new System.Text.ASCIIEncoding().GetBytes($"{username}:{password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            try
            {
                // Read the file content
                byte[] fileBytes = File.ReadAllBytes(localFilePath);

                // Create content to upload
                using (ByteArrayContent byteContent = new ByteArrayContent(fileBytes))
                {
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    // Upload file
                    HttpResponseMessage response = await client.PutAsync(new Uri(new Uri(webdavUrl), remoteFileName), byteContent);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("File uploaded successfully.");
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error: {response.StatusCode} - {errorContent}");
                    }
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
