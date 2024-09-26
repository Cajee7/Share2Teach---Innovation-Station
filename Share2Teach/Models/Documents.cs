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
        public double File_Size { get; set; }
        public required string File_Url {get; set;}

        public required string File_Type{get; set;}
        public string Moderation_Status {get; set;} = "Unmoderated";
        public int Ratings {get; set;}
        
         public List<string> Tags { get; set; } = new List<string>();
        public DateTime Date_Uploaded { get; set; }

        public DateTime? Date_Updated { get; set; }
    }
}
