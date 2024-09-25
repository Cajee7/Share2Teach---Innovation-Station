  // DTO for updating report status
    public class UpdateReportStatusDto
    {
        public required string Report_Status { get; set; }  // Status: true for approved, false for rejected
    }