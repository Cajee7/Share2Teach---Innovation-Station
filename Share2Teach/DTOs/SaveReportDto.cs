// DTO for saving report
using MongoDB.Bson;

public class SaveReportDto
    {
        public required string  DocumentId { get; set; }
        public required string Reason { get; set; }

        //public required string Report_status{get;set;}
    }