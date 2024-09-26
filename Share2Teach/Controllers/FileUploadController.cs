using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Document_Model.Models;
using Document_Model.DTOs;

namespace DatabaseConnection.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly IMongoCollection<Documents> _documentsCollection;
        private static readonly string username = "aramsunar";
        private static readonly string password = "Jaedene12!";
        private static readonly string webdavUrl = "http://localhost:8080/remote.php/dav/files/aramsunar/";

        public FileUploadController(IMongoClient client)
        {
            var database = client.GetDatabase("Share2Teach"); // Your database name
            _documentsCollection = database.GetCollection<Documents>("Documents");
        }

        // Upload file to Nextcloud, convert to PDF if necessary, and store metadata in MongoDB
        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument([FromForm] FileDownloadDto uploadDto)
        {
            if (uploadDto.File == null || uploadDto.File.Length == 0)
                return BadRequest(new { message = "No file provided." });

            // Get file information
            var fileName = Path.GetFileName(uploadDto.File.FileName);
            var fileSize = uploadDto.File.Length;
            var fileType = Path.GetExtension(fileName).ToLowerInvariant();

            // Define the folder where files will be saved locally for conversion
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var filePath = Path.Combine(uploadPath, fileName);

            // Save the original file locally
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await uploadDto.File.CopyToAsync(stream);
            }

            // Convert the file to PDF if needed
            var convertedFilePath = filePath; // Default to original file path if it's already a PDF
            if (fileType != ".pdf")
            {
                convertedFilePath = Path.Combine(uploadPath, Path.GetFileNameWithoutExtension(fileName) + ".pdf");

                // Convert Word files to PDF
                if (fileType == ".doc" || fileType == ".docx")
                {
                    var doc = new Aspose.Words.Document(filePath);
                    doc.Save(convertedFilePath, Aspose.Words.SaveFormat.Pdf);
                }
                // TODO: Add conversion for PowerPoint files if needed

                // Optionally delete the original file after conversion
                System.IO.File.Delete(filePath);
            }

            // Now upload the file (converted or original) to Nextcloud
            var uploadUrl = $"{webdavUrl}/{Path.GetFileName(convertedFilePath)}";

            using (var client = new HttpClient())
            {
                var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                using (var content = new StreamContent(new FileStream(convertedFilePath, FileMode.Open)))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    try
                    {
                        var response = await client.PutAsync(uploadUrl, content);

                        if (response.IsSuccessStatusCode)
                        {
                            // File uploaded successfully, now store metadata in MongoDB
                            var document = new Documents
                            {
                                Title = uploadDto.Title,
                                Subject = uploadDto.Subject,
                                Grade = int.Parse(uploadDto.Grade),
                                Description = uploadDto.Description,
                                File_Size = Math.Round((double)uploadDto.File.Length / (1024 * 1024), 2), // Size in MB
                                File_Url = uploadUrl, // Nextcloud URL
                                File_Type = ".pdf", // Final file type is PDF
                                Date_Uploaded = DateTime.UtcNow,
                                Tags = uploadDto.Tags ?? new List<string>(), // Optional tags
                                Ratings = 0, // Initial rating
                                Moderation_Status = "Unmoderated" // Default moderation status
                            };

                            await _documentsCollection.InsertOneAsync(document);

                            // Optionally delete the converted file from local storage
                            if (System.IO.File.Exists(convertedFilePath))
                            {
                                System.IO.File.Delete(convertedFilePath);
                            }

                            return Ok(new { message = "File uploaded, converted to PDF, and metadata saved successfully." });
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

        // GET: api/files (Retrieve all documents from MongoDB)
        [HttpGet]
        public async Task<IActionResult> GetAllDocuments()
        {
            try
            {
                var documents = await _documentsCollection.Find(Builders<Documents>.Filter.Empty).ToListAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
