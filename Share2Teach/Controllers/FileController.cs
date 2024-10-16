using AsposeWordsDocument = Aspose.Words.Document; // Alias for Aspose.Words.Document
using AsposePdfDocument = Aspose.Pdf.Document;     // Alias for Aspose.Pdf.Document
using Aspose.Pdf.Text; 
using Microsoft.AspNetCore.Authorization;
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
        private static readonly string username = "InnovationStation"; // Nextcloud username
        private static readonly string password = "IS_S2T24"; // Nextcloud password
         private static readonly string webdavUrl = "https://innovationstation.ddns.net/remote.php/dav/files/InnovationStation/Uploads/"; // Nextcloud WebDAV endpoint

        private const string LicenseFileName = "Share2Teach_-_Document_-Licence.pdf";

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

            // Creating separate index for tags to allow for efficient searches
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
        [Authorize(Roles = "admin, moderator, teacher")]
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

                string newFilePath = null;
                List<string> tags = new List<string>(); // List to hold tags

                // Handle Word documents
                if (fileType == ".doc" || fileType == ".docx")
                {
                    using (var wordStream = request.UploadedFile.OpenReadStream())
                    {
                        var asposeDoc = new AsposeWordsDocument(wordStream);
                        var pdfFileName = Path.GetFileNameWithoutExtension(fileName) + ".pdf";
                        newFilePath = Path.Combine(Path.GetTempPath(), pdfFileName);

                        // Save the document as PDF
                        asposeDoc.Save(newFilePath, Aspose.Words.SaveFormat.Pdf);

                        // Extract text from Word document for tag generation
                        var documentText = asposeDoc.GetText();
                        tags = GenerateTags(documentText);
                        Console.WriteLine("Generated Tags (Word): " + string.Join(", ", tags));

                        // Update fileName to the new PDF file
                        fileName = pdfFileName;
                        fileType = ".pdf";
                    }
                }
                else if (fileType == ".pdf")
                {
                    // Handle PDF files
                    newFilePath = Path.Combine(Path.GetTempPath(), fileName);
                    using (var fileStream = System.IO.File.Create(newFilePath))
                    {
                        await request.UploadedFile.CopyToAsync(fileStream);
                    }

                    // Extract text from PDF
                    var pdfDocument = new AsposePdfDocument(newFilePath);
                    var textAbsorber = new TextAbsorber();
                    pdfDocument.Pages.Accept(textAbsorber);
                    var documentText = textAbsorber.Text;
                    tags = GenerateTags(documentText);
                    Console.WriteLine("Generated Tags (PDF): " + string.Join(", ", tags));
                }
                
                // Create a temporary file for the license
                string relativePath = @"C:\Users\OEM\OneDrive\Desktop\Share2Teach---Innovation-Station\Share2Teach\bin\Debug\net8.0\Licenses\Share2Teach_-_Document_-Licence.pdf"; // Relative path
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory; // Base directory of the application
                string licenseFilePath = Path.Combine(baseDirectory, relativePath); // Full path to the license file

                // Create a new combined PDF with the license as the first page
                string combinedFilePath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(fileName)}_combined.pdf");
                using (var combinedDocument = new AsposePdfDocument())
                {
                    // Add license page first
                    combinedDocument.Pages.Add(new Aspose.Pdf.Document(licenseFilePath).Pages[1]);

                    // Add uploaded document pages
                    combinedDocument.Pages.Add(new Aspose.Pdf.Document(newFilePath).Pages);
                    combinedDocument.Save(combinedFilePath);
                }

                // Construct the new file name
                string newFileName = $"{request.Title}{request.Subject}{request.Grade}{fileType}";
                var encodedNewFileName = Uri.EscapeDataString(newFileName);
                var uploadUrl = $"{webdavUrl}{encodedNewFileName}";

                // Upload combined file to Nextcloud
                using (var client = new HttpClient())
                {
                    var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    using (var content = new StreamContent(System.IO.File.OpenRead(combinedFilePath)))
                    {
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                        var response = await client.PutAsync(uploadUrl, content);
                        if (!response.IsSuccessStatusCode)
                        {
                            return StatusCode((int)response.StatusCode, new { message = $"Upload to Nextcloud failed: {response.StatusCode}" });
                        }
                    }
                }

                // Clean up temporary files
                System.IO.File.Delete(newFilePath);
                System.IO.File.Delete(combinedFilePath);

                // Create a new document record to save in MongoDB
                var newDocument = new Documents
                {
                    Title = request.Title,
                    Subject = request.Subject,
                    Grade = request.Grade,
                    Description = request.Description,
                    File_Size = Math.Round(fileSize / (1024.0 * 1024.0), 2),
                    File_Url = uploadUrl,
                    File_Type = fileType,
                    Moderation_Status = "Unmoderated",
                    Date_Uploaded = DateTime.UtcNow,
                    Ratings = 0,
                    Tags = tags
                };

                await _documentsCollection.InsertOneAsync(newDocument);
                return Ok(new { message = $"File '{request.Title} { request.Subject}' uploaded to Nextcloud and metadata stored in MongoDB successfully.",
                Tags = tags });
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
                "the", ".", "is", "in", "at", "of", "and", "a", "to", "with", "that", "for", "it", "on", "this", 
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
        /// Searches for moderated documents based on the provided search query, including tags.
        /// </summary>
        /// <param name="request">The search request containing the query string.</param>
        /// <returns>An IActionResult containing the search results or an error message.</returns>
        [HttpGet("Search")]
        [AllowAnonymous]
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
                var filters = new List<FilterDefinition<Documents>>
                {
                    moderationFilter // Start with the moderation filter
                };

                // Split the search query into words
                var queryParts = request.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Check for specific grade, tag, or subject
                foreach (var part in queryParts)
                {
                    if (int.TryParse(part.Replace("grade", "").Replace("gr", "").Trim(), out int gradeQuery))
                    {
                        var gradeFilter = Builders<Documents>.Filter.Eq(d => d.Grade, gradeQuery);
                        filters.Add(gradeFilter);
                    }
                    else if (part.StartsWith("tag:"))
                    {
                        var tag = part.Substring(4); // Extract the tag after "tag:"
                        var tagsFilter = Builders<Documents>.Filter.AnyEq(d => d.Tags, tag);
                        filters.Add(tagsFilter);
                    }
                    else
                    {
                        // Add text search filter for Title, Description, Tags, and Subject
                        var textFilter = Builders<Documents>.Filter.Or(
                            Builders<Documents>.Filter.Text(part), // Title and Description
                            Builders<Documents>.Filter.Eq(d => d.Subject, part) // Subject
                        );
                        filters.Add(textFilter);
                    }
                }

                // Combine all filters using AND logic
                var combinedFilter = Builders<Documents>.Filter.And(filters);

                // Perform the search and project the required fields, including File_Url
                var result = await _documentsCollection
                    .Find(combinedFilter)
                    .Project(d => new
                    {
                        d.Title,
                        d.Subject,
                        d.Grade,
                        d.Description,
                        d.File_Size,
                        d.File_Url, // Include File_Url in the projection
                        d.Ratings,
                        d.Tags,
                        d.Date_Uploaded,
                        d.Date_Updated
                    })
                    .ToListAsync();

                if (!result.Any())
                {
                    return NotFound(new { message = "No documents found matching the search criteria." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

    

        /// <summary>
        /// Updates a document with the specified ID in MongoDB without modifying Nextcloud.
        /// </summary>
        /// <param name="id">The ID of the document to update.</param>
        /// <param name="updateDocumentDto">The DTO containing updated document information.</param>
        /// <returns>An IActionResult indicating the result of the update operation.</returns>
        /// <response code="204">No Content - Document updated successfully.</response>
        /// <response code="400">Bad Request - UpdateDocumentDto cannot be null.</response>
        /// <response code="404">Not Found - Document with specified ID not found.</response>
        /// <response code="500">Internal Server Error - An error occurred while updating the document.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "admin, moderator, teacher")]
        public async Task<IActionResult> UpdateDocument(string id, [FromBody] UpdateDocumentDto updateDocumentDto)
        {
            // Validate input
            if (updateDocumentDto == null)
            {
                return BadRequest("UpdateDocumentDto cannot be null.");
            }

            // Retrieve the existing document from MongoDB
            var document = await _documentsCollection.Find(d => d.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();

            if (document == null)
            {
                return NotFound($"Document with ID {id} not found.");
            }

            try
            {
                // Update fields in MongoDB
                document.Title = updateDocumentDto.Title;
                document.Subject = updateDocumentDto.Subject;
                document.Grade = updateDocumentDto.Grade;
                document.Description = updateDocumentDto.Description;
                document.Ratings = updateDocumentDto.Ratings;

                // Update the date updated
                document.Date_Updated = DateTime.UtcNow;

                // Update the document in the MongoDB database
                var updateResult = await _documentsCollection.ReplaceOneAsync(d => d.Id == document.Id, document);

                if (updateResult.IsAcknowledged && updateResult.ModifiedCount > 0)
                {
                    return Ok("Document updated successfully.");
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update the document.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception (consider using a logging framework)
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Downloads a document from Nextcloud based on the provided document ID.
        /// </summary>
        /// <param name="id">The ID of the document to download.</param>
        /// <returns>An IActionResult to handle the download.</returns>
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadDocument(string id)
        {
            try
            {
                var document = await _documentsCollection.Find(d => d.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
                if (document == null)
                {
                    return NotFound(new { message = "Document not found." });
                }

                // Create an HTTP client for downloading the file from Nextcloud
                using (var client = new HttpClient())
                {
                    var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    // Send a GET request to download the file
                    var response = await client.GetAsync(document.File_Url);
                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode, new { message = $"Download failed: {response.StatusCode}" });
                    }

                    var fileStream = await response.Content.ReadAsStreamAsync();
                    var fileName = Path.GetFileName(document.File_Url); // Get the file name from the URL

                    // Return the file as a downloadable response
                    return File(fileStream, "application/octet-stream", fileName);
                }
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
        [Authorize(Roles = "admin, moderator")]
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