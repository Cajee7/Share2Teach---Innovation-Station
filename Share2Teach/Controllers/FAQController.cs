using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Share2Teach.Models;
using LogController.Controllers; // For logging

namespace FAQApp.Controllers
{
    /// <summary>
    /// Provides an API for managing Frequently Asked Questions (FAQs).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FAQController : BaseLogController<FAQController> // Inherit from BaseLogController
    {
        private readonly IMongoCollection<BsonDocument> _faqCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="FAQController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public FAQController(ILogger<FAQController> logger) : base(logger)
        {
            var client = new MongoClient("mongodb+srv://muhammedcajee29:RU2AtjQc0d8ozPdD@share2teach.vtehmr8.mongodb.net/");
            var database = client.GetDatabase("Share2Teach");
            _faqCollection = database.GetCollection<BsonDocument>("FAQS");
        }

        /// <summary>
        /// Adds a new FAQ.
        /// </summary>
        /// <param name="faqInput">The FAQ input model containing question and answer.</param>
        /// <returns>A success message upon successful addition.</returns>
        /// <response code="200">If the FAQ is added successfully.</response>
        /// <response code="400">If the request is invalid.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("add")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "admin")]
        public IActionResult AddFAQ([FromBody] FAQS faqInput)
        {
            // Validate that the input contains the required fields
            if (string.IsNullOrEmpty(faqInput.Question) || string.IsNullOrEmpty(faqInput.Answer))
            {
                _logger.LogWarning("Attempted to add an FAQ with missing fields at {Timestamp}.", DateTime.UtcNow);
                return BadRequest("Question and Answer are required fields.");
            }

            // Create a new FAQ document with the automatically generated ObjectId and current DateTime
            var faqDocument = new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, // Automatically generated ObjectId
                { "question", faqInput.Question },
                { "answer", faqInput.Answer },
                { "dateAdded", DateTime.UtcNow } // Automatically generated DateAdded field
            };

            try
            {
                // Insert the document into the FAQ collection
                _faqCollection.InsertOne(faqDocument);
                _logger.LogInformation("Added FAQ: {Question} at {Timestamp}.", faqInput.Question, DateTime.UtcNow);

                return Ok("FAQ added successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while adding FAQ: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while adding the FAQ.");
            }
        }    

        
        /// <summary>
        /// Retrieves a list of all FAQs.
        /// </summary>
        /// <returns>A list of FAQs with their details.</returns>
        /// <response code="200">Returns the list of FAQs.</response>
        /// <response code="404">If no FAQs are found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("list")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetAllFAQs()
        {
            try
            {
                // Fetch all documents from the FAQS collection
                var faqs = _faqCollection.Find(new BsonDocument()).ToList();

                // Check if the collection is empty
                if (faqs.Count == 0)
                {
                    _logger.LogInformation("No FAQs found in the database.");
                    return NotFound("No FAQs found.");
                }

                // Convert to a model that includes and handles missing fields
                var faqList = faqs.Select(faq => new
                {
                    Id = faq["_id"].ToString(),
                    Question = faq.Contains("question") ? faq["question"].ToString() : "No question field",
                    Answer = faq.Contains("answer") ? faq["answer"].ToString() : "No answer field",
                    DateAdded = faq.Contains("dateAdded") ? faq["dateAdded"].ToUniversalTime() : DateTime.MinValue
                }).ToList();

                return Ok(faqList);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while fetching FAQs: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while fetching FAQs.");
            }
        }

        /// <summary>
        /// Deletes an FAQ by its ObjectId.
        /// </summary>
        /// <param name="id">The ObjectId of the FAQ to delete.</param>
        /// <returns>A success message upon successful deletion.</returns>
        /// <response code="200">If the FAQ is deleted successfully.</response>
        /// <response code="400">If the ObjectId format is invalid.</response>
        /// <response code="404">If the FAQ with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpDelete("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "admin")]
        public IActionResult DeleteFAQById([FromQuery] string id)
        {
            if (!ObjectId.TryParse(id, out ObjectId objectId))
            {
                return BadRequest("Invalid ObjectId format.");
            }

            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
            var result = _faqCollection.DeleteOne(filter);

            if (result.DeletedCount == 0)
            {
                _logger.LogWarning("FAQ with id '{Id}' not found for deletion at {Timestamp}.", id, DateTime.UtcNow);
                return NotFound("FAQ with the specified id not found.");
            }

            _logger.LogInformation("Deleted FAQ with id: {Id} at {Timestamp}.", id, DateTime.UtcNow);
            return Ok("FAQ deleted successfully.");
        }

        /// <summary>
        /// Updates an existing FAQ by its ObjectId.
        /// </summary>
        /// <param name="id">The ObjectId of the FAQ to update.</param>
        /// <param name="faqInput">The updated FAQ input model containing new question and answer.</param>
        /// <returns>A success message upon successful update.</returns>
        /// <response code="200">If the FAQ is updated successfully.</response>
        /// <response code="400">If the ObjectId format is invalid or required fields are missing.</response>
        /// <response code="404">If the FAQ with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPut("update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "admin")]
        public IActionResult UpdateFAQById([FromQuery] string id, [FromBody] FAQS faqInput)
        {
            if (!ObjectId.TryParse(id, out ObjectId objectId))
            {
                return BadRequest("Invalid ObjectId format.");
            }

            if (string.IsNullOrEmpty(faqInput.Question) || string.IsNullOrEmpty(faqInput.Answer))
            {
                _logger.LogWarning("Attempted to update FAQ with missing fields at {Timestamp}.", DateTime.UtcNow);
                return BadRequest("Both new question and answer fields are required.");
            }

            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
            var update = Builders<BsonDocument>.Update
                .Set("question", faqInput.Question)
                .Set("answer", faqInput.Answer)
                .Set("dateUpdated", DateTime.UtcNow);

            var result = _faqCollection.UpdateOne(filter, update);

            if (result.MatchedCount == 0)
            {
                _logger.LogWarning("FAQ with id '{Id}' not found for update at {Timestamp}.", id, DateTime.UtcNow);
                return NotFound("FAQ with the specified id not found.");
            }

            _logger.LogInformation("Updated FAQ with id: {Id} at {Timestamp}.", id, DateTime.UtcNow);
            return Ok("FAQ updated successfully.");
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetFAQById(string id)
        {
            if (!ObjectId.TryParse(id, out ObjectId objectId))
            {
                return BadRequest("Invalid ObjectId format.");
            }

            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
            var faq = _faqCollection.Find(filter).FirstOrDefault();

            if (faq == null)
            {
                return NotFound("FAQ with the specified id not found.");
            }

            return Ok(new { 
                question = faq["question"].AsString, 
                answer = faq["answer"].AsString 
            });
        }


    }
}