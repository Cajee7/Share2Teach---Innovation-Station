using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc;

namespace FAQApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FAQController : ControllerBase
    {
        private readonly IMongoCollection<BsonDocument> _faqCollection;

        public FAQController()
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
                return BadRequest("Question and Answer are required fields.");
            }

            var faqDocument = new BsonDocument
            {
                { "question", faqInput.Question },
                { "answer", faqInput.Answer },
                { "dateAdded", DateTime.UtcNow }
            };

            _faqCollection.InsertOne(faqDocument);

            return Ok("FAQ added successfully.");
        }

        // DELETE endpoint to delete an FAQ by ID
        [HttpDelete("delete/{id}")]
        public IActionResult DeleteFAQ(string id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(id));
            var result = _faqCollection.DeleteOne(filter);

            if (result.DeletedCount == 0)
            {
                return NotFound("FAQ not found.");
            }

            return Ok("FAQ deleted successfully.");
        }

        // PUT endpoint to update an FAQ by ID
        [HttpPut("update/{id}")]
        public IActionResult UpdateFAQ(string id, [FromBody] FAQInputModel faqInput)
        {
            if (string.IsNullOrEmpty(faqInput.Question) || string.IsNullOrEmpty(faqInput.Answer))
            {
                return BadRequest("Question and Answer are required fields.");
            }

            var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(id));
            var update = Builders<BsonDocument>.Update
                .Set("question", faqInput.Question)
                .Set("answer", faqInput.Answer)
                .Set("dateUpdated", DateTime.UtcNow);  // Optionally store the date of the update

            var result = _faqCollection.UpdateOne(filter, update);

            if (result.ModifiedCount == 0)
            {
                return NotFound("FAQ not found or no changes made.");
            }

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
