using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public class ReportDto
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)] // Map to MongoDB's "_id" field
    public string Id { get; set; } // This represents the report ID in MongoDB

    [BsonElement("DocumentId")]
    public required string DocumentId { get; set; } // ID of the reported document

    [BsonElement("Reason")]
    public required string Reason { get; set; } // Reason for reporting the document

    [BsonElement("Status")]
    public string Status { get; set; } = "pending"; // Default status is "pending"

    [BsonElement("DateReported")]
    public DateTime DateReported { get; set; } = DateTime.UtcNow; // Automatically set the date
}

