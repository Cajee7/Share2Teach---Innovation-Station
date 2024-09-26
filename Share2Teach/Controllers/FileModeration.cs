using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moderation.Models;
using Document_Model.Models;
using MongoDB.Bson;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

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
            _moderationCollection = database.GetCollection<ModerationEntry>("Moderations");
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
        [HttpPut("update/{documentId}")]
        [Authorize] // Ensure this endpoint requires authorization
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
                    .CurrentDate("Date_Updated");

                var result = await _documentsCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    return NotFound("Document not found or status not changed.");
                }

                // Extract moderator's name and ID from JWT token claims
                var moderatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var moderatorName = User.FindFirst(ClaimTypes.Name)?.Value; // This should contain the moderator's name
                var userId = User.FindFirst("user_id")?.Value; // Assuming you have a claim for the user_id

                if (string.IsNullOrEmpty(moderatorId) || string.IsNullOrEmpty(moderatorName))
                {
                    return Unauthorized("Moderator information is missing in the token.");
                }

                // Insert the moderation entry
                var moderationEntry = new ModerationEntry
                {
                    Moderator_id = moderatorId, // Store the moderator's ID
                    Moderator_Name = moderatorName, // Store the moderator's name for easy retrieval
                    User_id = userId,
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

        // GET: api/moderation/current-user
        [HttpGet("current-user")]
        [Authorize] // Ensure this reads token put into authorization
        public IActionResult GetCurrentUser()
        {
            // Retrieves the users' information from the token
            var email = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var firstName = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                Email = email,
                Name = firstName,
                Role = role
            });
        }
    }
}