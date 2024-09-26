
    // DTO for report submission with required fields
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

    public class CreateReportDto
    {
        public required string DocumentId { get; set; } // ID of the document being reported (required)
        public required string Reason { get; set; }     // Reason for reporting the document (required)
    }