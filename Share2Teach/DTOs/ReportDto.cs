public class ReportDto
    {
        public int Report_id { get; set; }
        public required string DocumentId { get; set; }
        public required string UserId { get; set; }
        public  required string Reason { get; set; }
        public required string Comment { get; set; }
        public required string Subject { get; set; }
    }