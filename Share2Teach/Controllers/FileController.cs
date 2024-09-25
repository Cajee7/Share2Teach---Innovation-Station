using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using Document_Model.Models;
using System.Collections.Generic;

namespace Combined.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    
    {
        private static readonly string username = "aramsunar"; // Nextcloud username
        private static readonly string password = "Jaedene12!"; // Nextcloud password
        private static readonly string webdavUrl = "http://localhost:8080/remote.php/dav/files/aramsunar/"; // Nextcloud WebDAV endpoint

        private readonly IMongoCollection<Documents> _documentsCollection;

        // Allowed file types
        private readonly List<string> _allowedFileTypes = new List<string> { ".doc", ".docx", ".pdf", ".ppt", ".pptx" };
        private const double MaxFileSizeMb = 25.0; // 25 MB limit

        public FileController(IMongoDatabase database)
        {
            _documentsCollection = database.GetCollection<Documents>("Documents");
        }

        // POST: api/file/upload
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] DocumentUploadRequest request)
        {
            try
            {
                // Check if file is provided
                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest(new { message = "No file was uploaded." });
                }

                // Get file information
                var fileName = Path.GetFileName(request.File.FileName);
                var fileSize = request.File.Length;
                var fileType = Path.GetExtension(fileName).ToLowerInvariant();

                // Check file size
                if (fileSize > MaxFileSizeMb * 1024 * 1024)
                {
                    return BadRequest(new { message = $"File size exceeds the limit of {MaxFileSizeMb} MB." });
                }

                // Check file type
                if (!_allowedFileTypes.Contains(fileType))
                {
                    return BadRequest(new { message = $"File type '{fileType}' is not allowed. Allowed types are: {string.Join(", ", _allowedFileTypes)}" });
                }

                // Upload file to Nextcloud
                var uploadUrl = $"{webdavUrl}{fileName}";
                using (var client = new HttpClient())
                {
                    var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    using (var content = new StreamContent(request.File.OpenReadStream()))
                    {
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                        var response = await client.PutAsync(uploadUrl, content);

                        if (!response.IsSuccessStatusCode)
                        {
                            return StatusCode((int)response.StatusCode, new { message = $"Upload to Nextcloud failed: {response.StatusCode}" });
                        }
                    }
                }

                // Create a new document record to save in MongoDB
                var newDocument = new Documents
                {
                    Title = request.Title,
                    Subject = request.Subject,
                    Grade = request.Grade,
                    Description = request.Description,
                    File_Size = Math.Round(fileSize / (1024.0 * 1024.0), 2), // Convert size to MB
                    File_Url = uploadUrl, // Save the Nextcloud link
                    File_Type = fileType, // Save original file type
                    Moderation_Status = "Unmoderated", // Initial moderation status
                    Date_Uploaded = DateTime.UtcNow,
                    Ratings = 0, // Initial rating
                    Tags = new List<string>() // Can be populated later
                };

                // Insert the document record into MongoDB
                await _documentsCollection.InsertOneAsync(newDocument);

                return Ok(new { message = $"File '{fileName}' uploaded to Nextcloud and metadata stored in MongoDB successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }
    }

    public class DocumentUploadRequest
    {
        public IFormFile File { get; set; }
        public string Title { get; set; }
        public string Subject { get; set; }
        public int Grade { get; set; }
        public string Description { get; set; }
    }
}
