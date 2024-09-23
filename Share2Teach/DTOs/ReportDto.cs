using MongoDB.Bson;

public class ReportDto
    {
        public int Report_id { get; set; }
        public int DocumentId { get; set; }
        public int UserId { get; set; }
        public  required string Reason { get; set; }
        //public required string Comment { get; set; }
        public required string Subject { get; set; }
    }