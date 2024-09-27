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
    [ApiController] // Indicates that this class will handle API request
    [Route("api/[controller]")] // Defines the base route for this controller as 'api/moderation'
    public class ModerationController : ControllerBase
    {
        // MongoDB collections for documents and moderation entries 
        private readonly IMongoCollection<Documents> _documentsCollection;
        private readonly IMongoCollection<ModerationEntry> _moderationCollection;

        // Constructor to inject MongoDB collections (dependencies) via dependency injection
        public ModerationController(IMongoDatabase database)
        {
            _documentsCollection = database.GetCollection<Documents>("Documents"); // Access the 'Documents' collection
            _moderationCollection = database.GetCollection<ModerationEntry>("Moderations"); // Access the 'Moderations' collection
        }

        // GET: api/moderation/unmoderated
        // Endpoint to get all unmoderated documents
        [HttpGet("unmoderated")]
        public async Task<IActionResult> GetUnmoderatedDocuments()
        {
            try
            {
                // Create a filter to search for documents with the status 'Unmoderated'
                var filter = Builders<Documents>.Filter.Eq(doc => doc.Moderation_Status, "Unmoderated");
                
                // Fetch all documents matching the filter
                var unmoderatedDocuments = await _documentsCollection.Find(filter).ToListAsync();
                
                // Return the list of unmoderated documents
                return Ok(unmoderatedDocuments);
            }
            catch (Exception ex)
            {
                // If something goes wrong, return a 500 internal server error with the exception message
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/moderation/update/{documentId}
        // Endpoint to update the moderation status of a document (requires authorization)
        [HttpPut("update/{documentId}")]
        [Authorize] // Requires the user to be authorized (JWT token)
        public async Task<IActionResult> UpdateModerationStatus(string documentId, [FromBody] UpdateModerationRequest request)
        {
            // Check if the request body is null
            if (request == null)
            {
                return BadRequest("Update request is null."); // Return bad request if request is invalid
            }

            try
            {
                // Parse the documentId into MongoDB's ObjectId format
                var objectId = ObjectId.Parse(documentId);
                
                // Filter to find the document by its ID
                var filter = Builders<Documents>.Filter.Eq(doc => doc.Id, objectId);

                // Update statement to change the moderation status and set the current date
                var update = Builders<Documents>.Update
                    .Set(doc => doc.Moderation_Status, request.Status) // Update the moderation status
                    .CurrentDate("Date_Updated"); // Automatically set the current date for 'Date_Updated'

                // Execute the update operation
                var result = await _documentsCollection.UpdateOneAsync(filter, update);

                // Check if any document was modified
                if (result.ModifiedCount == 0)
                {
                    return NotFound("Document not found or status not changed."); // Return not found if no document was updated
                }

                // Extract the moderator's information from the JWT token claims
                var moderatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Moderator's ID
                var moderatorName = User.FindFirst(ClaimTypes.Name)?.Value; // Moderator's name
                var userId = User.FindFirst("user_id")?.Value; // User ID who uploaded the document (assuming it's stored in claims)

                // If moderator information is missing, return an Unauthorized response
                if (string.IsNullOrEmpty(moderatorId) || string.IsNullOrEmpty(moderatorName))
                {
                    return Unauthorized("Moderator information is missing in the token.");
                }

                // Create a new moderation entry to log the moderation activity
                var moderationEntry = new ModerationEntry
                {
                    Moderator_id = moderatorId, // Store the moderator's ID
                    Moderator_Name = moderatorName, // Store the moderator's name
                    User_id = userId, // Store the user ID
                    Document_id = documentId, // Document ID
                    Date = DateTime.UtcNow, // Set the date to the current UTC time
                    Comments = request.Comment, // Any comments from the moderator
                    Ratings = null // Ratings can be set to null or any default value if not applicable
                };

                // Insert the moderation entry into the database
                await _moderationCollection.InsertOneAsync(moderationEntry);

                // Return success response
                return Ok("Document status updated and moderation entry added.");
            }
            catch (FormatException)
            {
                // If the documentId is not in the correct format, return a 400 Bad Request
                return BadRequest("Invalid document ID format.");
            }
            catch (Exception ex)
            {
                // For any other errors, return a 500 Internal Server Error
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/moderation/current-user
        // Endpoint to get the current user's information based on the JWT token (requires authorization)
        [HttpGet("current-user")]
        [Authorize] // Requires the user to be authorized (JWT token)
        public IActionResult GetCurrentUser()
        {
            // Extract user information from the JWT token
            var email = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Get user's email or ID (NameIdentifier)
            var firstName = User.FindFirst(ClaimTypes.Name)?.Value; // Get user's name
            var role = User.FindFirst(ClaimTypes.Role)?.Value; // Get user's role (e.g., Moderator, Admin, etc.)

            // Return the user's information in the response
            return Ok(new
            {
                Email = email, // Return user's email or ID
                Name = firstName, // Return user's name
                Role = role // Return user's role
            });
        }
    }
}
