using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moderation.Models;
using Document_Model.Models;


namespace FileModeration.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModerationController : ControllerBase
    {
        private readonly IMongoCollection<Documents> _documentsCollection;

        public ModerationController(IMongoDatabase database)
        {
            _documentsCollection = database.GetCollection<Documents>("Documents");
        }

        // GET: api/moderation/unmoderated
        [HttpGet("unmoderated")]
        public async Task<IActionResult> GetUnmoderatedDocuments()
        {
            try
            {
                // Fetch all documents where ModerationStatus is "Unmoderated"
                var filter = Builders<Documents>.Filter.Eq(doc => doc.Moderation_Status, "Unmoderated");
                var unmoderatedDocuments = await _documentsCollection.Find(filter).ToListAsync();

                // Return the list of unmoderated documents
                return Ok(unmoderatedDocuments);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}