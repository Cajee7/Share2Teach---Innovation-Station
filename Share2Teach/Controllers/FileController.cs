using Aspose.Words; // For handling Word documents
using Aspose.Pdf; // For handling PDF documents
using Aspose.Pdf.Facades; // For PDF manipulation
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Aliases to resolve ambiguity between Aspose.Words.Document and Aspose.Pdf.Document
using AsposeWordsDocument = Aspose.Words.Document;
using AsposePdfDocument = Aspose.Pdf.Document;

namespace Combined.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly ILogger<FileController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMongoCollection<Documents> _documentsCollection;

        // Configuration settings
        private readonly string _username;
        private readonly string _password;
        private readonly string _webdavUrl;
        private readonly string _licensePdfPath;
        private readonly List<string> _allowedFileTypes;
        private readonly double _maxFileSizeMb;

        public FileController(IMongoDatabase database, IConfiguration configuration, ILogger<FileController> logger)
        {
            _documentsCollection = database.GetCollection<Documents>("Documents");
            _configuration = configuration;
            _logger = logger;

            // Read configuration settings
            _username = _configuration["Nextcloud:Username"];
            _password = _configuration["Nextcloud:Password"];
            _webdavUrl = _configuration["Nextcloud:WebdavUrl"];
            _licensePdfPath = _configuration["FileSettings:LicensePdfPath"];
            _maxFileSizeMb = double.Parse(_configuration["FileSettings:MaxFileSizeMb"]);
            _allowedFileTypes = _configuration.GetSection("FileSettings:AllowedFileTypes").Get<List<string>>();

            // Ensure the text index is created for the fields used in search
            var indexKeys = Builders<Documents>.IndexKeys.Text(d => d.Title)
                                       .Text(d => d.Description)
                                       .Text(d => d.Tags);

            var indexModel = new CreateIndexModel<Documents>(indexKeys);
            _documentsCollection.Indexes.CreateOne(indexModel);
        }

        // POST: api/file/upload
        [HttpPost("upload")]
        [Authorize(Roles = "teacher")]
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
                var originalFileName = Path.GetFileName(request.UploadedFile.FileName);
                var fileSize = request.UploadedFile.Length;
                var fileType = Path.GetExtension(originalFileName).ToLowerInvariant();

                // Check file size
                if (fileSize > _maxFileSizeMb * 1024 * 1024)
                {
                    return BadRequest(new { message = $"File size exceeds the limit of {_maxFileSizeMb} MB." });
                }

                // Check file type
                if (!_allowedFileTypes.Contains(fileType))
                {
                    return BadRequest(new { message = $"File type '{fileType}' is not allowed. Allowed types are: {string.Join(", ", _allowedFileTypes)}" });
                }

                // Convert Word file to PDF if needed
                string convertedPdfPath = null;
                List<string> tags = new List<string>(); // List to hold tags
                if (fileType == ".doc" || fileType == ".docx")
                {
                    // Load the document using Aspose.Words
                    var asposeDoc = new AsposeWordsDocument(request.UploadedFile.OpenReadStream());

                    // Generate PDF file name
                    var pdfFileName = Path.GetFileNameWithoutExtension(originalFileName) + ".pdf";
                    convertedPdfPath = Path.Combine(Path.GetTempPath(), pdfFileName);

                    // Save the document as PDF
                    asposeDoc.Save(convertedPdfPath, Aspose.Words.SaveFormat.Pdf);

                    // Extract text from the document for tag generation
                    var documentText = asposeDoc.ToString(Aspose.Words.SaveFormat.Text);

                    // Generate tags from document text (basic implementation: top 10 frequent words excluding stopwords)
                    tags = GenerateTags(documentText);
                    _logger.LogInformation("Generated Tags: {Tags}", string.Join(", ", tags));

                    // Update fileName to the new PDF file
                    originalFileName = pdfFileName;
                    fileType = ".pdf";
                }

                // Construct the new file name
                string newFileName = $"{request.Title}_{request.Subject}_{request.Grade}{fileType}";

                // Combine PDFs (license and converted document)
                string finalPdfPath = Path.Combine(Path.GetTempPath(), "Final_" + newFileName);
                using (FileStream finalPdfStream = new FileStream(finalPdfPath, FileMode.Create))
                {
                    // Load the license PDF
                    var licensePdf = new AsposePdfDocument(_licensePdfPath);

                    // Create an output PDF document
                    var outputPdf = new AsposePdfDocument();

                    // Import pages from license PDF
                    outputPdf.Pages.Add(licensePdf.Pages);

                    if (convertedPdfPath != null && System.IO.File.Exists(convertedPdfPath))
                    {
                        // Load the converted PDF
                        var convertedPdf = new AsposePdfDocument(convertedPdfPath);
                        // Import pages from converted PDF
                        outputPdf.Pages.Add(convertedPdf.Pages);
                    }
                    else
                    {
                        // If the original file is already a PDF and no conversion was needed
                        var originalPdf = new AsposePdfDocument(request.UploadedFile.OpenReadStream());
                        outputPdf.Pages.Add(originalPdf.Pages);
                    }

                    // Save the final PDF with license
                    outputPdf.Save(finalPdfStream);
                }

                // Encode the new file name to handle spaces and special characters
                var encodedNewFileName = Uri.EscapeDataString(newFileName);

                // Upload file to Nextcloud (final PDF with license)
                var uploadUrl = $"{_webdavUrl}{encodedNewFileName}";
                using (var client = new HttpClient())
                {
                    var byteArray = Encoding.ASCII.GetBytes($"{_username}:{_password}");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    using (var content = new StreamContent(System.IO.File.OpenRead(finalPdfPath)))
                    {
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                        var response = await client.PutAsync(uploadUrl, content);

                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogError("Upload to Nextcloud failed with status code: {StatusCode}", response.StatusCode);
                            return StatusCode((int)response.StatusCode, new { message = $"Upload to Nextcloud failed: {response.StatusCode}" });
                        }
                    }
                }

                // Clean up temporary files
                if (convertedPdfPath != null && System.IO.File.Exists(convertedPdfPath))
                {
                    System.IO.File.Delete(convertedPdfPath);
                }
                if (System.IO.File.Exists(finalPdfPath))
                {
                    System.IO.File.Delete(finalPdfPath);
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

                return Ok(new { message = $"File '{newFileName}' uploaded to Nextcloud and metadata stored in MongoDB successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during file upload: {ErrorMessage}", ex.Message);
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
            if (word.EndsWith("ing") && word.Length > 4)
            {
                return word.Substring(0, word.Length - 3);  // Example: "running" -> "run"
            }
            if (word.EndsWith("ed") && word.Length > 3)
            {
                return word.Substring(0, word.Length - 2);  // Example: "played" -> "play"
            }
            if (word.EndsWith("s") && word.Length > 3)
            {
                return word.Substring(0, word.Length - 1);  // Example: "cats" -> "cat"
            }

            // Return the word as-is if no rule applies
            return word;
        }

        // GET: api/file/search
        [HttpGet("search")]
        [Authorize] // Optional: Add authorization if needed
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
                _logger.LogInformation("Filter: {Filter}", renderedFilter.ToJson());

                // Project only the fields we want to show, including a Download URL pointing to the API's download endpoint
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
                    Download_Url = Url.Action(nameof(DownloadFile), "File", new { fileName = $"{d.Title}_{d.Subject}_{d.Grade}{d.File_Type}" }, Request.Scheme)
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
                _logger.LogError("Error during search: {ErrorMessage}", ex.Message);
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        // GET: api/file/download/{fileName}
        [HttpGet("download/{fileName}")]
        [Authorize] // Optional: Add authorization to secure the download endpoint
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            try
            {
                // Encode the file name to handle spaces and special characters
                var encodedFileName = Uri.EscapeDataString(fileName);
                var downloadUrl = $"{_webdavUrl}{encodedFileName}";

                using (var client = new HttpClient())
                {
                    // Adding basic authentication headers
                    var byteArray = Encoding.ASCII.GetBytes($"{_username}:{_password}");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    var response = await client.GetAsync(downloadUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("Download from Nextcloud failed with status code: {StatusCode}", response.StatusCode);
                        return StatusCode((int)response.StatusCode, new { message = $"Download from Nextcloud failed: {response.StatusCode}" });
                    }

                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

                    return File(fileBytes, contentType, fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during download: {ErrorMessage}", ex.Message);
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        // GET: api/file/moderated
        [HttpGet("moderated")]
        [Authorize] // Optional: Add authorization if needed
        public async Task<IActionResult> GetModeratedFiles()
        {
            try
            {
                // Filter to get documents with Moderation_Status set to "Moderated"
                var filter = Builders<Documents>.Filter.Eq(d => d.Moderation_Status, "Moderated");

                // Project only the fields we want to show, including a Download URL
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
                    Download_Url = Url.Action(nameof(DownloadFile), "File", new { fileName = $"{d.Title}_{d.Subject}_{d.Grade}{d.File_Type}" }, Request.Scheme)
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
                _logger.LogError("Error retrieving moderated files: {ErrorMessage}", ex.Message);
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }
    }
}
