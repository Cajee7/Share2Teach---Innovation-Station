using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moderation.Models;
using Document_Model.Models;
using MongoDB.Bson;

namespace FileModeration.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModerationController : ControllerBase
    {
        private readonly IMongoCollection<Documents> _documentsCollection;
        private readonly IMongoCollection<ModerationEntry> _moderationCollection;

        public ModerationController(IMongoDatabase database)
        {
            _documentsCollection = database.GetCollection<Documents>("Documents");
            _moderationCollection = database.GetCollection<ModerationEntry>("Moderations"); // Assuming you have a Moderations collection
        }

        // GET: api/moderation/unmoderated
        [HttpGet("unmoderated")]
        public async Task<IActionResult> GetUnmoderatedDocuments()
        {
            try
            {
                var filter = Builders<Documents>.Filter.Eq(doc => doc.Moderation_Status, "Unmoderated");
                var unmoderatedDocuments = await _documentsCollection.Find(filter).ToListAsync();
                return Ok(unmoderatedDocuments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/moderation/update/{documentId}
        // PUT: api/moderation/update/{documentId}
[HttpPut("update/{documentId}")]
public async Task<IActionResult> UpdateModerationStatus(string documentId, [FromBody] UpdateModerationRequest request)
{
    if (request == null)
    {
        return BadRequest("Update request is null.");
    }

    try
    {
        // Parse documentId to ObjectId
        var objectId = ObjectId.Parse(documentId);
        var filter = Builders<Documents>.Filter.Eq(doc => doc.Id, objectId); // Use ObjectId directly
        
        var update = Builders<Documents>.Update
            .Set(doc => doc.Moderation_Status, request.Status)
            .CurrentDate("DateUpdated"); // Assuming you have a field for the last updated date

        var result = await _documentsCollection.UpdateOneAsync(filter, update);

        if (result.ModifiedCount == 0)
        {
            return NotFound("Document not found or status not changed.");
        }

        // Insert the moderation entry
        var moderationEntry = new ModerationEntry
        {
            Moderator_id = ""/* Get this from your authentication context */,
            User_id = ""/* Get this from your authentication context */,
            Document_id = documentId,
            Date = DateTime.UtcNow,
            Comments = request.Comment,
            Ratings = null // Set to null or default value if not applicable
        };

        await _moderationCollection.InsertOneAsync(moderationEntry);

        return Ok("Document status updated and moderation entry added.");
    }
    catch (FormatException)
    {
        return BadRequest("Invalid document ID format.");
    }
    catch (Exception ex)
    {
        return StatusCode(500, $"Internal server error: {ex.Message}");
    }
}

    }
}
