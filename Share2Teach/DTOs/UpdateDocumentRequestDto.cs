public class UpdateDocumentRequest
    {
        public required string Title { get; set; }
        public required string Subject { get; set; }
        public int Grade { get; set; }
        public required string Description { get; set; }
    }