using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; 
using LogController.Controllers; 

namespace FAQApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FAQController : BaseLogController // Inherit from BaseLogController
    {
        private readonly IMongoCollection<BsonDocument> _faqCollection;

        public FAQController(ILogger<FAQController> logger) : base(logger)
        {
            var client = new MongoClient("mongodb+srv://muhammedcajee29:RU2AtjQc0d8ozPdD@share2teach.vtehmr8.mongodb.net/");
            var database = client.GetDatabase("Share2Teach");
            _faqCollection = database.GetCollection<BsonDocument>("FAQS");
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

            var faqDocument = new BsonDocument
            {
                { "question", faqInput.Question },
                { "answer", faqInput.Answer },
                { "dateAdded", DateTime.UtcNow }
            };

            _faqCollection.InsertOne(faqDocument);
            _logger.LogInformation("Added FAQ: {Question} at {Timestamp}.", faqInput.Question, DateTime.UtcNow); // Log the added FAQ

            return Ok("FAQ added successfully.");
        }

        // DELETE endpoint to delete an FAQ by question
        [HttpDelete("delete")]
        public IActionResult DeleteFAQ([FromQuery] string question)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("question", question);
            var result = _faqCollection.DeleteOne(filter);

            if (result.DeletedCount == 0)
            {
                _logger.LogWarning("FAQ with question '{Question}' not found for deletion at {Timestamp}.", question, DateTime.UtcNow);
                return NotFound("FAQ with the specified question not found.");
            }

            _logger.LogInformation("Deleted FAQ: {Question} at {Timestamp}.", question, DateTime.UtcNow); // Log the deleted FAQ
            return Ok("FAQ deleted successfully.");
        }

        // PUT endpoint to update an FAQ by question
        [HttpPut("update")]
        public IActionResult UpdateFAQ([FromQuery] string question, [FromBody] FAQInputModel faqInput)
        {
            if (string.IsNullOrEmpty(faqInput.Question) || string.IsNullOrEmpty(faqInput.Answer))
            {
                _logger.LogWarning("Attempted to update FAQ with missing fields at {Timestamp}.", DateTime.UtcNow);
                return BadRequest("Both new question and answer fields are required.");
            }

            var filter = Builders<BsonDocument>.Filter.Eq("question", question);
            var update = Builders<BsonDocument>.Update
                .Set("question", faqInput.Question)  // new question
                .Set("answer", faqInput.Answer)      // new answer
                .Set("dateUpdated", DateTime.UtcNow);  // date of update

            var result = _faqCollection.UpdateOne(filter, update);

            if (result.MatchedCount == 0)
            {
                _logger.LogWarning("FAQ with question '{Question}' not found for update at {Timestamp}.", question, DateTime.UtcNow);
                return NotFound("FAQ with the specified question not found.");
            }

            _logger.LogInformation("Updated FAQ: {OldQuestion} to {NewQuestion} at {Timestamp}.", question, faqInput.Question, DateTime.UtcNow); // Log the updated FAQ
            return Ok("FAQ updated successfully.");
        }
    }

    // Model for input validation
    public class FAQInputModel
    {
        public string? Question { get; set; }
        public string? Answer { get; set; }
    }
}