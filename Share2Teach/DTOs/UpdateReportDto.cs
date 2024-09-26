  // DTO for updating report status
    public class UpdateReportDto
    {
        public required string Status { get; set; }  // Status: true for approved, false for rejected
    }