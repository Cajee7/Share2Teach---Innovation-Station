using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Share2Teach.Models
{
    public class FAQS
    {
        [BsonId] // This will map to the _id field in MongoDB
        [BsonRepresentation(BsonType.ObjectId)] // Ensures proper ObjectId handling

        [BsonElement("question")] // This will map to the "question" field in the document
        public required string Question { get; set; }

        [BsonElement("answer")] // This will map to the "answer" field in the document
        public required string Answer { get; set; }
    }
}
