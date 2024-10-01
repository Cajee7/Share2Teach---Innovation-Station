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
using System.Text.RegularExpressions;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="FileController"/> class.
        /// </summary>
        /// <param name="database">The MongoDB database instance.</param>
        public FileController(IMongoDatabase database)
        {
            _documentsCollection = database.GetCollection<Documents>("Documents");

            // Ensure the text index is created for the fields used in search
            var indexKeys = Builders<Documents>.IndexKeys.Text(d => d.Title)
                                       .Text(d => d.Description)
                                       .Text(d => d.Tags);

            var indexModel = new CreateIndexModel<Documents>(indexKeys);
            _documentsCollection.Indexes.CreateOne(indexModel);

            //Creating seperate index for tags to allow for efficient searches
            var tagsIndex = Builders<Documents>.IndexKeys.Ascending(d => d.Tags);
            var tagsIndexModel = new CreateIndexModel<Documents>(tagsIndex);
            _documentsCollection.Indexes.CreateOne(tagsIndexModel);

            // Creating indexes for Grade and Subject
            var gradeIndex = Builders<Documents>.IndexKeys.Ascending(d => d.Grade);
            var subjectIndex = Builders<Documents>.IndexKeys.Ascending(d => d.Subject);
            var gradeIndexModel = new CreateIndexModel<Documents>(gradeIndex);
            var subjectIndexModel = new CreateIndexModel<Documents>(subjectIndex);
            _documentsCollection.Indexes.CreateOne(gradeIndexModel);
            _documentsCollection.Indexes.CreateOne(subjectIndexModel);

            
        }

        /// <summary>
        /// Uploads a file to Nextcloud and stores its metadata in MongoDB.
        /// </summary>
        /// <param name="request">The combined upload request containing file and metadata.</param>
        /// <returns>An action result indicating the outcome of the upload operation.</returns>
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
                string newFileName = $"{request.Title}{request.Subject}{request.Grade}{fileType}";

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

        /// <summary>
        /// Generates tags based on the document text by filtering out stopwords and selecting the most frequent words.
        /// </summary>
        /// <param name="documentText">The text content of the document.</param>
        /// <returns>A list of generated tags.</returns>
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

        /// <summary>
        /// Lemmatizes a given word based on basic rules.
        /// </summary>
        /// <param name="word">The word to be lemmatized.</param>
        /// <returns>The lemmatized form of the word.</returns>
        private string LemmatizeWord(string word)
        {
            // Simple rules for lemmatization (this can be enhanced)
            if (word.EndsWith("s"))
                return word.Substring(0, word.Length - 1); // Remove plural 's'
            if (word.EndsWith("ed") || word.EndsWith("ing"))
                return word.Substring(0, word.Length - 2); // Remove 'ed' or 'ing'
            return word; // Return as is
        }

        /// <summary>
        /// Searches for moderated documents based on the provided search query.
        /// </summary>
        /// <param name="request">The search request containing the query string.</param>
        /// <returns>An IActionResult containing the search results or an error message.</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchDocuments([FromQuery] SearchRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Query))
                {
                    return BadRequest(new { message = "Search query cannot be empty." });
                }

                // Create a filter for moderated documents
                var moderationFilter = Builders<Documents>.Filter.Eq(d => d.Moderation_Status, "Moderated");

                // Initialize filters
                FilterDefinition<Documents> subjectFilter = FilterDefinition<Documents>.Empty;
                FilterDefinition<Documents> gradeFilter = FilterDefinition<Documents>.Empty;

                // Split the search query into words
                var queryParts = request.Query.Split(' ');

                // Check for a specific grade
                foreach (var part in queryParts)
                {
                    if (int.TryParse(part.Replace("grade", "").Replace("gr", "").Trim(), out int gradeQuery))
                    {
                        gradeFilter = Builders<Documents>.Filter.Eq(d => d.Grade, gradeQuery);
                    }
                    else
                    {
                        // Assuming the subject is the first non-numeric part
                        subjectFilter = Builders<Documents>.Filter.Eq(d => d.Subject, part);
                    }
                }

                // Combine all filters using $and to ensure both subject and grade match
                var combinedFilter = Builders<Documents>.Filter.And(
                    moderationFilter,
                    subjectFilter,
                    gradeFilter
                );

                // Log the filter (optional)
                var renderedFilter = combinedFilter.Render(_documentsCollection.DocumentSerializer, _documentsCollection.Settings.SerializerRegistry);
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
                    .Find(combinedFilter)
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


        /// <summary>
        /// Deletes a document with the specified ID and its associated file from Nextcloud.
        /// </summary>
        /// <param name="id">The ID of the document to delete.</param>
        /// <returns>An IActionResult indicating the outcome of the delete operation.</returns>
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteDocument(string id)
        {
            try
            {
                // Validate the provided ID
                if (!ObjectId.TryParse(id, out var objectId))
                {
                    return BadRequest(new { message = "Invalid document ID format." });
                }

                // Find the document in MongoDB
                var document = await _documentsCollection.Find(d => d.Id == objectId).FirstOrDefaultAsync();
                if (document == null)
                {
                    return NotFound(new { message = "Document not found." });
                }

                // Construct the URL for deleting the file from Nextcloud
                var fileName = $"{document.Title}{document.Subject}{document.Grade}{document.File_Type}";
                var encodedFileName = Uri.EscapeDataString(fileName);
                var deleteUrl = $"{webdavUrl}{encodedFileName}";

                using (var client = new HttpClient())
                {
                    // Adding basic authentication headers
                    var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    // Send DELETE request to Nextcloud
                    var response = await client.DeleteAsync(deleteUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode, new { message = $"Failed to delete file from Nextcloud: {response.StatusCode}" });
                    }
                }

                // Delete the document from MongoDB
                await _documentsCollection.DeleteOneAsync(d => d.Id == objectId);

                return Ok(new { message = "Document and associated file deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }
    }
}
