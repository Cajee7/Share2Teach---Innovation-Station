using MongoDB.Bson;

namespace Document_Model.Models
{
    public class Documents
    {
        public ObjectId Id {get; set;} // MongoDB ID
        public required string Title { get; set; }
        public required string Subject { get; set; }
        public int Grade { get; set; }
        public required string Description { get; set; }
        public double FileSize { get; set; }
        public required string FileUrl {get; set;}
        public string ModerationStatus {get; set;} = "Unmoderated";
        public int Ratings {get; set;}
        public List<string> Tags {get; set;} = new(); // Default to an empty list
        public DateTime DateUploaded { get; set; }
    }
}
