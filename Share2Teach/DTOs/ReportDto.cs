using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

/// <summary>
/// Data transfer object for a report.
/// </summary>
public class ReportDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the report.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the document identifier associated with the report.
    /// Treated as ObjectId in the database, but used as a string in the application.
    /// </summary>
    [BsonElement("DocumentId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the reason for the report.
    /// </summary>
    [BsonElement("Reason")]
    public required string Reason { get; set; }

    /// <summary>
    /// Gets or sets the status of the report.
    /// Default value is "pending".
    /// </summary>
    [BsonElement("Status")]
    public required string Status { get; set; } = "pending";

    /// <summary>
    /// Gets or sets the date and time when the report was made.
    /// </summary>
    [BsonElement("DateReported")]
    public DateTime DateReported { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportDto"/> class.
    /// Sets the <see cref="DateReported"/> property to the current UTC time.
    /// </summary>
    public ReportDto()
    {
        DateReported = DateTime.UtcNow;
    }
}
