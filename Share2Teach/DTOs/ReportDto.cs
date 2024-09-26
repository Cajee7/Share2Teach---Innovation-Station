using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class ReportDto
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("DocumentId")]
    [BsonRepresentation(BsonType.ObjectId)] // Treat DocumentId as ObjectId in the DB, string in the app
    public string DocumentId { get; set; }

    [BsonElement("Reason")]
    public string Reason { get; set; }

    [BsonElement("Status")]
    public string Status { get; set; } = "pending";

    [BsonElement("DateReported")]
    public DateTime DateReported { get; set; }

<<<<<<< Updated upstream
=======
    // Constructor to initialize DateReported with the current UTC time
    public ReportDto()
    {
        DateReported = DateTime.UtcNow;
    }
}
>>>>>>> Stashed changes
