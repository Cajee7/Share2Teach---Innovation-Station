using Aspose.Words;
using Aspose.Pdf; // Ensure you have this namespace for PDF operations
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MongoDB.Bson;
using Document_Model.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Linq;

namespace UploadDocuments.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IMongoCollection<Documents> _documentsCollection;

        // Allowed file types
        private readonly List<string> _allowedFileTypes = new List<string> { ".doc", ".docx", ".pdf", ".ppt", ".pptx" };
        private const double MaxFileSizeMb = 25.0; // 25 MB limit

        // Nextcloud configuration
        private readonly string _nextcloudBaseUri = "http://localhost:8080/remote.php/dav/files/aramsunar/";
        private readonly string _nextcloudUsername = "aramsunar";
        private readonly string _nextcloudPassword = "Jaedene12!";

        public FilesController(IMongoDatabase database)
        {
            _documentsCollection = database.GetCollection<Documents>("Documents");
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadRequest request)
        {
            try
            {
                // Check if file is provided
                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest("No file was uploaded.");
                }

                // Get file information
                var fileName = Path.GetFileName(request.File.FileName);
                var fileSize = request.File.Length;
                var fileType = Path.GetExtension(fileName).ToLowerInvariant();

                // Check file size (in bytes, 25 MB = 25 * 1024 * 1024 bytes)
                if (fileSize > MaxFileSizeMb * 1024 * 1024)
                {
                    return BadRequest($"File size exceeds the limit of {MaxFileSizeMb} MB.");
                }

                // Check file type
                if (!_allowedFileTypes.Contains(fileType))
                {
                    return BadRequest($"File type '{fileType}' is not allowed. Allowed types are: {string.Join(", ", _allowedFileTypes)}");
                }

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
                    await request.File.CopyToAsync(stream);
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

                // Upload the file to Nextcloud
                var uploadUrl = $"{_nextcloudBaseUri}{Path.GetFileName(convertedFilePath)}";

                using (var client = new HttpClient())
                {
                    var byteArray = Encoding.ASCII.GetBytes($"{_nextcloudUsername}:{_nextcloudPassword}");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    using (var content = new StreamContent(new FileStream(convertedFilePath, FileMode.Open)))
                    {
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                        var response = await client.PutAsync(uploadUrl, content);

                        if (!response.IsSuccessStatusCode)
                        {
                            return StatusCode((int)response.StatusCode, new { message = $"Upload to Nextcloud failed: {response.StatusCode}" });
                        }
                    }
                }

                // Extract text from the PDF for tag generation
                string extractedContent = ExtractTextFromPdf(convertedFilePath);
                List<string> generatedTags = GenerateTags(extractedContent);

                // Create a new document record to save in MongoDB
                var newDocument = new Documents
                {
                    Title = request.Title,
                    Subject = request.Subject,
                    Grade = request.Grade,
                    Description = request.Description,
                    File_Size = Math.Round(fileSize / (1024.0 * 1024.0), 2), // Convert size to MB
                    File_Url = uploadUrl, // URL of the file in Nextcloud
                    File_Type = ".pdf", // Updated file type to PDF
                    Moderation_Status = "Unmoderated", // Initial moderation status
                    Date_Uploaded = DateTime.UtcNow,
                    Ratings = 0, // Initial rating
                    Tags = generatedTags // Save generated tags
                };

                // Insert the document record into MongoDB
                await _documentsCollection.InsertOneAsync(newDocument);

                // Delete the locally saved file after uploading to Nextcloud
                if (System.IO.File.Exists(convertedFilePath))
                {
                    System.IO.File.Delete(convertedFilePath);
                }

                return Ok($"File '{fileName}' uploaded, converted to PDF, and stored on Nextcloud successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private string ExtractTextFromPdf(string pdfFilePath)
        {
            using (var document = new Aspose.Pdf.Document(pdfFilePath))
            {
                StringBuilder text = new StringBuilder();
                foreach (var page in document.Pages)
                {
                    text.Append(page.Contents);
                }
                return text.ToString();
            }
        }

        private List<string> GenerateTags(string content)
        {
            var words = content.Split(new[] { ' ', ',', '.', ';', ':', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var tagCounts = new Dictionary<string, int>();

            foreach (var word in words)
            {
                var lowerWord = word.ToLowerInvariant();
                if (lowerWord.Length > 3) // You can set a threshold for tag length
                {
                    if (tagCounts.ContainsKey(lowerWord))
                        tagCounts[lowerWord]++;
                    else
                        tagCounts[lowerWord] = 1;
                }
            }

            // Get top 5 most frequent words as tags
            var tags = tagCounts.OrderByDescending(kvp => kvp.Value)
                                .Take(5)
                                .Select(kvp => kvp.Key)
                                .ToList();

            return tags;
        }

        // GET: api/files
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

        // DELETE: api/files/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(string id)
        {
            try
            {
                var objectId = new ObjectId(id);
                var deleteResult = await _documentsCollection.DeleteOneAsync(d => d.Id == objectId);

                if (deleteResult.DeletedCount == 0)
                {
                    return NotFound($"Document with ID {id} was not found.");
                }

                return Ok($"Document with ID {id} has been deleted successfully.");
            }
            catch (FormatException)
            {
                return BadRequest("Invalid ID format.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/files/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(string id, [FromBody] UpdateDocumentRequest updateRequest)
        {
            try
            {
                var objectId = new ObjectId(id);

                // Fetch the document to update
                var existingDocument = await _documentsCollection.Find(d => d.Id == objectId).FirstOrDefaultAsync();
                if (existingDocument == null)
                {
                    return NotFound($"Document with ID {id} was not found.");
                }

                // Update the allowed fields
                existingDocument.Title = updateRequest.Title;
                existingDocument.Subject = updateRequest.Subject;
                existingDocument.Grade = updateRequest.Grade;
                existingDocument.Description = updateRequest.Description;
                existingDocument.Date_Uploaded = DateTime.UtcNow; // Update the Date field

                // Save the updated document in the database
                var updateResult = await _documentsCollection.ReplaceOneAsync(d => d.Id == objectId, existingDocument);

                if (updateResult.MatchedCount == 0)
                {
                    return NotFound($"Failed to update Document with ID {id}.");
                }

                return Ok($"Document with ID {id} has been updated successfully.");
            }
            catch (FormatException)
            {
                return BadRequest("Invalid ID format.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}