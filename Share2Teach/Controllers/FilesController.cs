using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Document_Model.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace UploadDocumentsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private const long MaxFileSize = 25 * 1024 * 1024; // 25 MB
        private const string NextcloudBaseUrl = "http://localhost:8080/remote.php/dav/files/MuhammedCajee29";
        private const string NextcloudUsername = "MuhammedCajee29";
        private const string NextcloudPassword = "Jaedene12!";
        private readonly ILogger<FilesController> _logger;

        public FilesController(ILogger<FilesController> logger)
        {
            _logger = logger;
        }

        // POST: api/files/upload
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] string title, [FromForm] string subject, [FromForm] string description, [FromForm] int grade)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Error: No file uploaded.");
            }

            if (file.Length > MaxFileSize)
            {
                return BadRequest("Error: File size exceeds 25 MB!");
            }

            var filePath = Path.GetTempFileName();
            var outputPdfPath = Path.ChangeExtension(filePath, ".pdf");

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Convert the file to PDF
                ConvertToPdf(filePath, outputPdfPath);

                // Upload the converted PDF to Nextcloud
                string nextcloudUrl = await UploadToNextcloud(outputPdfPath);
                if (string.IsNullOrEmpty(nextcloudUrl))
                {
                    return StatusCode(500, "Error uploading file to Nextcloud.");
                }

                // Create file metadata
                var document = new Documents
                {
                    Title = title,
                    Subject = subject,
                    Description = description,
                    FileSize = new FileInfo(outputPdfPath).Length,
                    FileUrl = nextcloudUrl,
                    DateUploaded = DateTime.UtcNow,
                    ModerationStatus = "Unmoderated",
                    Ratings = 0,
                    Tags = GenerateTags(outputPdfPath),
                    Grade = grade
                };

                // Save document metadata to MongoDB
                SaveDocumentToDatabase(document);

                return Ok(new { Message = "File uploaded successfully.", FileUrl = nextcloudUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred during file upload: {ex.Message}");
                return StatusCode(500, "An internal server error occurred.");
            }
            finally
            {
                // Clean up temporary files
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                if (System.IO.File.Exists(outputPdfPath)) System.IO.File.Delete(outputPdfPath);
            }
        }

        // DELETE: api/files/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteFile(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
    {
        return BadRequest("Invalid ID format.");
    }

    var database = DatabaseConnection.Program.ConnectToDatabase();
    var collection = database.GetCollection<Documents>("Documents");

    var result = collection.DeleteOne(d => d.Id == objectId);

    if (result.DeletedCount == 0)
    {
        return NotFound($"Document with ID {id} not found.");
    }

    return Ok(new { Message = "Document deleted successfully." });
        }

        // PUT: api/files/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFile(string id, [FromForm] IFormFile file, [FromForm] string title, [FromForm] string subject, [FromForm] string description, [FromForm] int grade)
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest("Invalid ID format.");
            }

            var database = DatabaseConnection.Program.ConnectToDatabase();
            var collection = database.GetCollection<Documents>("Documents");

            var document = await collection.Find(d => d.Id == objectId).FirstOrDefaultAsync();
            if (document == null)
            {
                return NotFound($"Document with ID {id} not found.");
            }

            if (file != null && file.Length > 0)
            {
                if (file.Length > MaxFileSize)
                {
                    return BadRequest("Error: File size exceeds 25 MB!");
                }

                var filePath = Path.GetTempFileName();
                var outputPdfPath = Path.ChangeExtension(filePath, ".pdf");

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Convert the file to PDF
                    ConvertToPdf(filePath, outputPdfPath);

                    // Upload the converted PDF to Nextcloud
                    string nextcloudUrl = await UploadToNextcloud(outputPdfPath);
                    if (string.IsNullOrEmpty(nextcloudUrl))
                    {
                        return StatusCode(500, "Error uploading file to Nextcloud.");
                    }

                    document.FileUrl = nextcloudUrl;
                    document.FileSize = new FileInfo(outputPdfPath).Length;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error occurred during file update: {ex.Message}");
                    return StatusCode(500, "An internal server error occurred.");
                }
                finally
                {
                    // Clean up temporary files
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                    if (System.IO.File.Exists(outputPdfPath)) System.IO.File.Delete(outputPdfPath);
                }
            }

            // Update document metadata
            document.Title = title;
            document.Subject = subject;
            document.Description = description;
            document.Grade = grade;

            // Update the document in the database
            var updateResult = await collection.ReplaceOneAsync(d => d.Id == objectId, document);
            if (updateResult.ModifiedCount == 0)
            {
                return NotFound($"Document with ID {id} not found or not modified.");
            }

            return Ok(new { Message = "Document updated successfully.", Document = document });
        }

        // POST, DELETE, and PUT methods remain the same...

        private static async Task<string> UploadToNextcloud(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string uploadUrl = $"{NextcloudBaseUrl}/{fileName}";

            using (HttpClient client = new HttpClient())
            {
                var byteArray = Encoding.ASCII.GetBytes($"{NextcloudUsername}:{NextcloudPassword}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                using (var content = new StreamContent(System.IO.File.OpenRead(filePath)))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    try
                    {
                        HttpResponseMessage response = await client.PutAsync(uploadUrl, content);
                        if (response.IsSuccessStatusCode)
                        {
                            return uploadUrl;
                        }
                        else
                        {
                            string errorMessage = await response.Content.ReadAsStringAsync();
                            throw new Exception($"Upload failed: {response.StatusCode} - {errorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error during file upload: {ex.Message}");
                    }
                }
            }
        }

        private static void ConvertToPdf(string filePath, string outputPdfPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "libreoffice",
                Arguments = $"--headless --convert-to pdf \"{filePath}\" --outdir \"{Path.GetDirectoryName(outputPdfPath)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string error = process.StandardError.ReadToEnd();
                    throw new Exception($"LibreOffice conversion failed: {error}");
                }
            }
        }

        private static List<string> GenerateTags(string filePath)
        {
            var tags = new List<string>();

            try
            {
                var fileContent = System.IO.File.ReadAllText(filePath);
                var words = fileContent.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in words)
                {
                    if (word.Length > 3)
                    {
                        tags.Add(word);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating tags: " + ex.Message);
            }

            return tags;
        }

        private static void SaveDocumentToDatabase(Documents document)
        {
            var database = DatabaseConnection.Program.ConnectToDatabase();
            if (database != null)
            {
                var collection = database.GetCollection<Documents>("Documents");
                collection.InsertOne(document);
            }
            else
            {
                throw new Exception("Failed to connect to the database.");
            }
        }
    }
}
