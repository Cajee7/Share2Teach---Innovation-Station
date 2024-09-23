using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
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
                // Fetch all documents from the MongoDB collection
                var documents = await _documentsCollection.Find(Builders<Documents>.Filter.Empty).ToListAsync();

                // Return the list of documents
                return Ok(documents);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}