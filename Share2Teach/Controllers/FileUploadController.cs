using Microsoft.AspNetCore.Mvc;//working with the web API
using System;
using System.IO;
using System.Net.Http;//sedning HTTP requests and receiving HTTP requests 
using System.Net.Http.Headers;
using System.Text;//ASCII code
using System.Threading.Tasks;

namespace DatabaseConnection.Controllers
{
    [ApiController]// indicates that this class is a API controller 
    [Route("api/[controller]")]// the route template for the controller, and the controller name become Upload file 
    public class FileUploadcontroller : ControllerBase//makes API controler for handling HTTP Requests and reponses 
    {
        private static readonly string username = "aramsunar";// authentication into nextcloud 
        private static readonly string password = "Jaedene12!";
        private static readonly string webdavUrl = "http://localhost:8080/remote.php/dav/files/aramsunar/";//Nextcloud WebDav endpoint

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