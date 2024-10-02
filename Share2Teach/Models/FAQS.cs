using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Share2Teach.Models
{
    /// <summary>
    /// Represents a Frequently Asked Question (FAQ) in the system.
    /// </summary>
    public class FAQS
    {
        /// <summary>
        /// Gets or sets the unique identifier for the FAQ.
        /// This will map to the _id field in MongoDB.
        /// </summary>
        [BsonId] // This will map to the _id field in MongoDB
        [BsonRepresentation(BsonType.ObjectId)] // Ensures proper ObjectId handling
        public required string Id { get; set; }

        /// <summary>
        /// Gets or sets the question being asked.
        /// This will map to the "question" field in the document.
        /// </summary>
        [BsonElement("question")] // This will map to the "question" field in the document
        public required string Question { get; set; }

        /// <summary>
        /// Gets or sets the answer to the question.
        /// This will map to the "answer" field in the document.
        /// </summary>
        [BsonElement("answer")] // This will map to the "answer" field in the document
        public required string Answer { get; set; }
    }
}
