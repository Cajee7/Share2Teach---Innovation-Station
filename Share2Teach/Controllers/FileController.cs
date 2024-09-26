using Aspose.Words;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using Document_Model.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Search.Models;

namespace Combined.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private static readonly string username = "aramsunar"; // Nextcloud username
        private static readonly string password = "Jaedene12!";
        private static readonly string webdavUrl = "http://localhost:8080/remote.php/dav/files/aramsunar/"; // Nextcloud WebDAV endpoint

        private readonly IMongoCollection<Documents> _documentsCollection;

        // Allowed file types
        private readonly List<string> _allowedFileTypes = new List<string> { ".doc", ".docx", ".pdf", ".ppt", ".pptx" };
        private const double MaxFileSizeMb = 25.0; // 25 MB limit

        public FileController(IMongoDatabase database)
        {
            _documentsCollection = database.GetCollection<Documents>("Documents");

            // Ensure the text index is created for the fields used in search
            var indexKeys = Builders<Documents>.IndexKeys.Text(d => d.Title)
                                       .Text(d => d.Description)
                                       .Text(d => d.Tags);

            var indexModel = new CreateIndexModel<Documents>(indexKeys);
            _documentsCollection.Indexes.CreateOne(indexModel);
        }

        // POST: api/file/upload
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] CombinedUploadRequest request)
        {
            try
            {
                // Check if file is provided
                if (request.UploadedFile == null || request.UploadedFile.Length == 0)
                {
                    return BadRequest(new { message = "No file was uploaded." });
                }

                // Get file information
                var fileName = Path.GetFileName(request.UploadedFile.FileName);
                var fileSize = request.UploadedFile.Length;
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

                // Convert Word file to PDF if needed
                string pdfFilePath = null;
                List<string> tags = new List<string>(); // List to hold tags
                if (fileType == ".doc" || fileType == ".docx")
                {
                    // Load the document using Aspose.Words
                    var asposeDoc = new Document(request.UploadedFile.OpenReadStream());

                    // Generate PDF file name
                    var pdfFileName = Path.GetFileNameWithoutExtension(fileName) + ".pdf";
                    pdfFilePath = Path.Combine(Path.GetTempPath(), pdfFileName);

                    // Save the document as PDF
                    asposeDoc.Save(pdfFilePath);

                    // Extract text from the document for tag generation
                    var documentText = asposeDoc.ToString(SaveFormat.Text);

                    // Generate tags from document text (basic implementation: top 10 frequent words excluding stopwords)
                    tags = GenerateTags(documentText);
                    Console.WriteLine("Generated Tags: " + string.Join(", ", tags));

                    // Update fileName to the new PDF file
                    fileName = pdfFileName;
                    fileType = ".pdf";
                }

                // Construct the new file name
string newFileName = $"{request.Title}_{request.Subject}_{request.Grade}{fileType}";

                // Encode the new file name to handle spaces and special characters
var encodedNewFileName = Uri.EscapeDataString(newFileName);

                // Upload file to Nextcloud (PDF or original)
                var uploadUrl = $"{webdavUrl}{encodedNewFileName}";
                using (var client = new HttpClient())
                {
                    var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    using (var content = new StreamContent(pdfFilePath != null ? System.IO.File.OpenRead(pdfFilePath) : request.UploadedFile.OpenReadStream()))
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
                    File_Type = fileType, // Save the file type (PDF or original)
                    Moderation_Status = "Unmoderated", // Initial moderation status
                    Date_Uploaded = DateTime.UtcNow,
                    Ratings = 0, // Initial rating
                    Tags = tags // Tags generated from document content
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

        // Function to generate tags based on document text
        private List<string> GenerateTags(string documentText)
        {
            // Expanded stopword list (can be customized further)
            var stopWords = new HashSet<string>
            {
                "the", "is", "in", "at", "of", "and", "a", "to", "with", "that", "for", "it", "on", "this", 
                "by", "from", "or", "an", "as", "be", "was", "were", "has", "have", "are", "will", "would",
                "could", "should", "can", "but", "about", "which", "into", "if", "when", "they", "there",
                "their", "its", "these", "those", "i", "you", "he", "she", "we", "they", "them", "his",
                "her", "my", "our", "your", "us", "than", "so", "too", "then", "just", "any", "each",
                "every", "how", "who", "what", "where", "why", "again", "more", "no", "not", "do", "did",
                "me", "him", "up", "down", "all", "here", "over", "some", "only", "out", "now", "very",
                "such", "also"
            };

            // Split text into words and filter
            var words = documentText.Split(new[] { ' ', '\r', '\n', ',', '.', '!', '?', ';', ':', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(word => word.ToLowerInvariant())   // Convert to lowercase
                            .Where(word => word.All(char.IsLetter))   // Keep only alphabetic words
                            .Where(word => word.Length > 1)           // Filter out single-letter words
                            .Where(word => !stopWords.Contains(word)) // Filter stopwords
                            .GroupBy(word => LemmatizeWord(word))      // Group by base (lemmatized) form
                            .OrderByDescending(group => group.Count()) // Order by frequency
                            .Take(10)                                  // Top 10 most frequent words
                            .Select(group => group.Key)                // Select base form
                            .ToList();

            return words;
        }
        private string LemmatizeWord(string word)
        {
            // Basic lemmatization rules
            if (word.EndsWith("ing"))
            {
                return word.TrimEnd('i', 'n', 'g');  // Example: "running" -> "run"
            }
            if (word.EndsWith("ed"))
            {
                return word.TrimEnd('e', 'd');       // Example: "played" -> "play"
            }
            if (word.EndsWith("s") && word.Length > 3)
            {
                return word.TrimEnd('s');            // Example: "cats" -> "cat"
            }

            // Return the word as-is if no rule applies
            return word;
        }

        // POST: api/file/search
        [HttpGet("search")]
        public async Task<IActionResult> SearchDocuments([FromQuery] SearchRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Query))
                {
                    return BadRequest(new { message = "Search query cannot be empty." });
                }

                // Create a $text filter for the search query
                var filter = Builders<Documents>.Filter.And(
                Builders<Documents>.Filter.Eq(d => d.Moderation_Status, "Moderated"),
                Builders<Documents>.Filter.Text(request.Query)
                );

                // Log the filter (optional)
                var renderedFilter = filter.Render(_documentsCollection.DocumentSerializer, _documentsCollection.Settings.SerializerRegistry);
                Console.WriteLine($"Filter: {renderedFilter.ToJson()}");

                // Project only the fields we want to show
                var projection = Builders<Documents>.Projection.Expression(d => new
                {
                    d.Title,
                    d.Subject,
                    d.Grade,
                    d.Description,
                    d.File_Size,
                    d.Ratings,
                    d.Tags,
                    d.Date_Uploaded,
                    Download_Url = d.File_Url
                });

                // Perform the search query
                var documents = await _documentsCollection
                    .Find(filter)
                    .Project(projection)
                    .ToListAsync();

                // Check if results are found
                if (documents.Count == 0)
                {
                    return Ok(new { message = "No matching documents found." });
                }

                // Return the results
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }


        // GET: api/file/download/{fileName}
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            try
            {
                // Encode the file name to handle spaces and special characters
                var encodedFileName = Uri.EscapeDataString(fileName);
                var downloadUrl = $"{webdavUrl}{encodedFileName}";

                using (var client = new HttpClient())
                {
                    // Adding basic authentication headers
                    var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

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
        

        // GET: api/file/moderated
        [HttpGet("moderated")]
        public async Task<IActionResult> GetModeratedFiles()
        {
            try
            {
                // Filter to get documents with Moderation_Status set to "Moderated"
                var filter = Builders<Documents>.Filter.Eq(d => d.Moderation_Status, "Moderated");

                // Project only the fields we want to show
                var projection = Builders<Documents>.Projection.Expression(d => new
                {
                    d.Title,
                    d.Subject,
                    d.Grade,
                    d.Description,
                    d.File_Size,
                    d.Ratings,
                    d.Tags,
                    d.Date_Uploaded,
                });

                // Retrieve documents from the database
                var documents = await _documentsCollection
                    .Find(filter)
                    .Project(projection)
                    .ToListAsync();

                // Check if results are found
                if (documents.Count == 0)
                {
                    return Ok(new { message = "No moderated documents found." });
                }

                // Return the results
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }        
    }
}