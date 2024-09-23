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