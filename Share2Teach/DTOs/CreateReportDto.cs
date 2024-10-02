using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

/// <summary>
/// Data transfer object for creating a report submission with required fields.
/// </summary>
public class CreateReportDto
{
    /// <summary>
    /// Gets or sets the ID of the document being reported.
    /// This field is required.
    /// </summary>
    public required string DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the reason for reporting the document.
    /// This field is required.
    /// </summary>
    public required string Reason { get; set; }
}
