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
    /// <summary>
    /// API Controller for managing document moderation functionality.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ModerationController : ControllerBase
    {
        private readonly IMongoCollection<Documents> _documentsCollection;
        private readonly IMongoCollection<ModerationEntry> _moderationCollection;

        /// <summary>
        /// Constructor to initialize MongoDB collections for documents and moderation entries.
        /// </summary>
        /// <param name="database">The MongoDB database instance.</param>
        public ModerationController(IMongoDatabase database)
        {
            _documentsCollection = database.GetCollection<Documents>("Documents");
            _moderationCollection = database.GetCollection<ModerationEntry>("Moderations");
        }

        /// <summary>
        /// Retrieves all unmoderated documents.
        /// </summary>
        /// <returns>List of unmoderated documents.</returns>
        /// <response code="200">Returns the list of unmoderated documents.</response>
        /// <response code="500">If an internal server error occurs.</response>
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

        /// <summary>
        /// Updates the moderation status of a document.
        /// </summary>
        /// <param name="documentId">The ID of the document to be updated.</param>
        /// <param name="request">The request body containing status, comment, and rating.</param>
        /// <returns>Confirmation of status update and moderation entry addition.</returns>
        /// <response code="200">If the status was successfully updated and moderation entry was added.</response>
        /// <response code="400">If the request body is null or the document ID format is invalid.</response>
        /// <response code="401">If the user is unauthorized or moderator information is missing in the token.</response>
        /// <response code="404">If the document was not found or status was not changed.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPut("update/{documentId}")]
        [Authorize]
        public async Task<IActionResult> UpdateModerationStatus(string documentId, [FromBody] UpdateModerationRequest request)
        {
            if (request == null)
            {
                return BadRequest("Update request is null.");
            }

            try
            {
                var objectId = ObjectId.Parse(documentId);
                var filter = Builders<Documents>.Filter.Eq(doc => doc.Id, objectId);
                var update = Builders<Documents>.Update
                    .Set(doc => doc.Moderation_Status, request.Status)
                    .CurrentDate("Date_Updated");

                var result = await _documentsCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    return NotFound("Document not found or status not changed.");
                }

                var moderatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var moderatorName = User.FindFirst(ClaimTypes.Name)?.Value;
                var userId = User.FindFirst("user_id")?.Value;

                if (string.IsNullOrEmpty(moderatorId) || string.IsNullOrEmpty(moderatorName))
                {
                    return Unauthorized("Moderator information is missing in the token.");
                }

                var moderationEntry = new ModerationEntry
                {
                    Moderator_id = moderatorId,
                    Moderator_Name = moderatorName,
                    User_id = userId,
                    Document_id = documentId,
                    Date = DateTime.UtcNow,
                    Comments = request.Comment,
                    Ratings = request.Rating
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

        /// <summary>
        /// Retrieves the current user's information from the JWT token.
        /// </summary>
        /// <returns>The current user's email, name, and role.</returns>
        /// <response code="200">Returns the current user's information.</response>
        /// <response code="401">If the user is unauthorized.</response>
        [HttpGet("current-user")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
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
