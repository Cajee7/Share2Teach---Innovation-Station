using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LogController.Controllers; // For logging

namespace FAQApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FAQController : BaseLogController <FAQController> // Inherit from BaseLogController
    {
        private readonly IMongoCollection<BsonDocument> _faqCollection;

        public FAQController(ILogger<FAQController> logger) : base(logger)
        {
            var client = new MongoClient("mongodb+srv://muhammedcajee29:RU2AtjQc0d8ozPdD@share2teach.vtehmr8.mongodb.net/");
            var database = client.GetDatabase("Share2Teach");
            _faqCollection = database.GetCollection<BsonDocument>("FAQS");
        }

        // GET endpoint to list all FAQs with their IDs
        [HttpGet("list")]
        public IActionResult GetAllFAQs()
        {
            _logger.LogInformation("Fetching all FAQs at {Timestamp}.", DateTime.UtcNow);
            try
            {
                var faqs = _faqCollection.Find(new BsonDocument()).ToList();

                if (faqs.Count == 0)
                {
                    _logger.LogInformation("No FAQs found in the database at {Timestamp}.", DateTime.UtcNow);
                    return NotFound("No FAQs found.");
                }

                var faqList = faqs.Select(faq => new
                {
                    Id = faq["_id"].ToString(),
                    Question = faq.Contains("question") ? faq["question"].ToString() : "No question field",
                    Answer = faq.Contains("answer") ? faq["answer"].ToString() : "No answer field",
                    DateAdded = faq.Contains("dateAdded") ? faq["dateAdded"].ToUniversalTime() : DateTime.MinValue
                }).ToList();

                _logger.LogInformation("Successfully fetched {Count} FAQs at {Timestamp}.", faqList.Count, DateTime.UtcNow);
                return Ok(faqList);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while fetching FAQs at {Timestamp}: {Message}", DateTime.UtcNow, ex.Message);
                return StatusCode(500, "An error occurred while fetching FAQs.");
            }
        }

        // POST endpoint to add a new FAQ
        [HttpPost("add")]
        public IActionResult AddFAQ([FromBody] FAQInputModel faqInput)
        {
            if (string.IsNullOrEmpty(faqInput.Question) || string.IsNullOrEmpty(faqInput.Answer))
            {
                _logger.LogWarning("Attempted to add an FAQ with missing fields at {Timestamp}.", DateTime.UtcNow);
                return BadRequest("Question and Answer are required fields.");
            }

            try
            {
                var faqDocument = new BsonDocument
                {
                    { "question", faqInput.Question },
                    { "answer", faqInput.Answer },
                    { "dateAdded", DateTime.UtcNow }
                };

                _faqCollection.InsertOne(faqDocument);
                _logger.LogInformation("Added FAQ: {Question} at {Timestamp}.", faqInput.Question, DateTime.UtcNow);

                return Ok("FAQ added successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while adding FAQ at {Timestamp}: {Message}", DateTime.UtcNow, ex.Message);
                return StatusCode(500, "An error occurred while adding the FAQ.");
            }
        }

        // DELETE endpoint to delete an FAQ by ObjectId
        [HttpDelete("delete")]
        public IActionResult DeleteFAQById([FromQuery] string id)
        {
            _logger.LogInformation("Attempting to delete FAQ with id: {Id} at {Timestamp}.", id, DateTime.UtcNow);
            
            if (!ObjectId.TryParse(id, out ObjectId objectId))
            {
                _logger.LogWarning("Invalid ObjectId format: {Id} at {Timestamp}.", id, DateTime.UtcNow);
                return BadRequest("Invalid ObjectId format.");
            }

            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var result = _faqCollection.DeleteOne(filter);

                if (result.DeletedCount == 0)
                {
                    _logger.LogWarning("FAQ with id '{Id}' not found for deletion at {Timestamp}.", id, DateTime.UtcNow);
                    return NotFound("FAQ with the specified id not found.");
                }

                _logger.LogInformation("Successfully deleted FAQ with id: {Id} at {Timestamp}.", id, DateTime.UtcNow);
                return Ok("FAQ deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while deleting FAQ with id: {Id} at {Timestamp}: {Message}", id, DateTime.UtcNow, ex.Message);
                return StatusCode(500, "An error occurred while deleting the FAQ.");
            }
        }

        // PUT endpoint to update an FAQ by ObjectId
        [HttpPut("update")]
        public IActionResult UpdateFAQById([FromQuery] string id, [FromBody] FAQInputModel faqInput)
        {
            _logger.LogInformation("Attempting to update FAQ with id: {Id} at {Timestamp}.", id, DateTime.UtcNow);

            if (!ObjectId.TryParse(id, out ObjectId objectId))
            {
                _logger.LogWarning("Invalid ObjectId format: {Id} at {Timestamp}.", id, DateTime.UtcNow);
                return BadRequest("Invalid ObjectId format.");
            }

            if (string.IsNullOrEmpty(faqInput.Question) || string.IsNullOrEmpty(faqInput.Answer))
            {
                _logger.LogWarning("Attempted to update FAQ with missing fields at {Timestamp}.", DateTime.UtcNow);
                return BadRequest("Both new question and answer fields are required.");
            }

            try
            {
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

                _logger.LogInformation("Successfully updated FAQ with id: {Id} at {Timestamp}.", id, DateTime.UtcNow);
                return Ok("FAQ updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while updating FAQ with id: {Id} at {Timestamp}: {Message}", id, DateTime.UtcNow, ex.Message);
                return StatusCode(500, "An error occurred while updating the FAQ.");
            }
        }
    }

    // Model for input validation
    public class FAQInputModel
    {
        public string? Question { get; set; }
        public string? Answer { get; set; }
    }
}