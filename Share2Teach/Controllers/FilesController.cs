using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Threading.Tasks;
using MongoDB.Bson;
using Document_Model.Models;

namespace UploadDocuments.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IMongoCollection<Documents> _documentsCollection;

        public FilesController(IMongoDatabase database)
        {
            _documentsCollection = database.GetCollection<Documents>("Documents");
        }

        // POST: api/files/upload
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

                // Define the folder where files will be saved (this could be Nextcloud or another file storage location)
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Get file information
                var fileName = Path.GetFileName(request.File.FileName);
                var filePath = Path.Combine(uploadPath, fileName);
                var fileSize = request.File.Length;
                var fileType = Path.GetExtension(fileName);

                // Save the file locally (or to a cloud location)
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

                // Create a new document record to save in MongoDB
                var newDocument = new Documents
                {
                    Title = request.Title,
                    Subject = request.Subject,
                    Grade = request.Grade,
                    Description = request.Description,
                    File_Size = Math.Round(fileSize / (1024.0 * 1024.0), 2), // Convert size to MB
                    File_Url = filePath, // Ideally, this would be the URL to access the file in storage
                    File_Type = fileType,
                    Moderation_Status = "Unmoderated", // Initial moderation status
                    Date_Uploaded = DateTime.UtcNow,
                    Ratings = 0, // Initial rating
                    Tags = new List<string>() // Can be populated later
                };

                // Insert the document record into MongoDB
                await _documentsCollection.InsertOneAsync(newDocument);

                return Ok($"File '{fileName}' uploaded successfully and document added to the database.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
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