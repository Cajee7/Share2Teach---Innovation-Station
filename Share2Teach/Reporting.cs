using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportingController : ControllerBase
    {
        private static IMongoCollection<BsonDocument> GetReportCollection()
        {
            var database = DatabaseConnection.Program.ConnectToDatabase();
            return database.GetCollection<BsonDocument>("Reports");
        }

        private static readonly Dictionary<string, List<string>> moderatorSubjects = new Dictionary<string, List<string>>
        {
            { "moderator1@example.com", new List<string> { "Inappropriate content", "Outdated" } },
            { "moderator2@example.com", new List<string> { "Inaccurate", "Misleading" } }
        };

        private string GetCurrentModeratorEmail()
        {
            // Replace this with your actual logic to get the moderator's email
            return User.Identity?.Name ?? string.Empty;  // Assuming you're using claims-based authentication
        }

        // GET: api/reporting
        // GET: api/reporting/GetReports
        [HttpGet("GetReports")]
        public IActionResult GetReports()
        {
             var reportCollection = GetReportCollection();

             // No filters applied, fetch all reports
            var reports = reportCollection.Find(Builders<BsonDocument>.Filter.Empty).ToList();

             return Ok(reports);
        }
        
        // POST: api/reporting
        [HttpPost("CreateReport")]
        public IActionResult CreateReport([FromBody] ReportDto reportDto)
        {
            if (reportDto == null || reportDto.Report_id <= 0 || reportDto.DocumentId<=0 ||
                reportDto.UserId <=0 || string.IsNullOrEmpty(reportDto.Reason) ||
                string.IsNullOrEmpty(reportDto.Subject))
            {
                return BadRequest(new { message = "All fields are required." });
            }

            var reportCollection = GetReportCollection();
            var reportDocument = new BsonDocument
            {
                { "Report_id", reportDto.Report_id },
                { "DocumentId", reportDto.DocumentId },
                { "UserId", reportDto.UserId },
                { "Reason", reportDto.Reason },
               // { "Comment", reportDto.Comment },
                { "Subject", reportDto.Subject },
                { "Status", BsonNull.Value },
                { "DateSubmitted", DateTime.UtcNow }
            };

            reportCollection.InsertOne(reportDocument);
            return CreatedAtAction(nameof(GetReports), new { id = reportDto.Report_id }, reportDto);
        }

        // POST: api/reporting/review/{id}
        [HttpPost("review/{id}")]
        public IActionResult ReviewReport(int id, [FromBody] ReportReviewDto reviewDto)
        {
            var reportCollection = GetReportCollection();
            var filter = Builders<BsonDocument>.Filter.Eq("Report_id", id);

            var report = reportCollection.Find(filter).FirstOrDefault();
            if (report == null)
                return NotFound(new { message = "Report not found." });

            string moderatorEmail = GetCurrentModeratorEmail();

            // Check authorization
            if (!moderatorSubjects.ContainsKey(moderatorEmail) || !moderatorSubjects[moderatorEmail].Contains(report["Subject"].AsString))
            {
                return StatusCode(403, new { message = "You are not authorized to review this report." });

            }

            var update = Builders<BsonDocument>.Update
                .Set("Status", reviewDto.Approve)
                .Set("ModeratorComments", reviewDto.ModeratorComments)
                .Set("DateReviewed", DateTime.UtcNow);

            var result = reportCollection.UpdateOne(filter, update);
            if (result.MatchedCount > 0)
            {
                return Ok(new { message = "Report reviewed successfully." });
            }
            return BadRequest(new { message = "Report not reviewed. Report ID not found." });
        }
    }
}
    


/*using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace ReportManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportingController : ControllerBase
    {
        private static readonly List<ReportDto> ReportDatabase = new List<ReportDto>
        {
            new ReportDto { Report_id = 1, DocumentId = "file1.txt", UserId = "user1", Reason = "Inappropriate content", Comment = "Contains offensive material.", Status = null, DateSubmitted = DateTime.UtcNow, Subject = "Content Review" },
            new ReportDto { Report_id = 2, DocumentId = "file2.txt", UserId = "user2", Reason = "Outdated", Comment = "Information is outdated.", Status = null, DateSubmitted = DateTime.UtcNow, Subject = "Technical Review" },
            new ReportDto { Report_id = 3, DocumentId = "file3.txt", UserId = "user3", Reason = "Inaccurate", Comment = "Incorrect data.", Status = true, DateSubmitted = DateTime.UtcNow, ModeratorComments = "Confirmed issue.", Subject = "Content Review" }
        };

        // GET: api/reporting/unreviewed
        [HttpGet("unreviewed")]
        public IActionResult GetUnreviewedReports([FromQuery] string documentId = null)
        {
            var unreviewedReports = ReportDatabase.Where(r => r.Status == null);

            if (!string.IsNullOrEmpty(documentId))
            {
                unreviewedReports = unreviewedReports.Where(r => r.DocumentId.Equals(documentId, StringComparison.OrdinalIgnoreCase));
            }

            return Ok(unreviewedReports.ToList());
        }

        // GET: api/reporting/{id}
        [HttpGet("{id}")]
        public IActionResult GetReport(int id)
        {
            var report = ReportDatabase.FirstOrDefault(r => r.Report_id == id);
            if (report == null)
                return NotFound(new { message = "Report not found." });

            return Ok(report);
        }

        // POST: api/reporting
        [HttpPost("SubmitReport")]
        public IActionResult SubmitReport([FromBody] ReportDto newReport)
        {
            if (newReport == null || newReport.Report_id <= 0 || string.IsNullOrEmpty(newReport.Reason))
                return BadRequest(new { message = "Please provide all required information (Report_id and Reason)." });

            if (ReportDatabase.Any(r => r.Report_id == newReport.Report_id))
                return Conflict(new { message = "A report with this Report_id already exists." });

            newReport.DateSubmitted = DateTime.UtcNow;
            ReportDatabase.Add(newReport);

            return CreatedAtAction(nameof(GetReport), new { id = newReport.Report_id }, newReport);
        }

        // POST: api/reporting/review/{id}
[HttpPost("review/{id}")]
public IActionResult ReviewReport(int id, [FromBody] ReportReviewDto reviewDto)
{
    var report = ReportDatabase.FirstOrDefault(r => r.Report_id == id);
    if (report == null)
        return NotFound(new { message = "Report not found." });

    // Check if the report has already been reviewed
    if (report.Status.HasValue)
        return BadRequest(new { message = "This report has already been reviewed." });

    // Retrieve the moderator's assigned subjects from claims
    var moderatorSubjects = User.FindFirstValue("Subjects");

    if (string.IsNullOrEmpty(moderatorSubjects))
    {
        return Unauthorized(new { message = "Moderator subjects not found in claims." });
    }

    // Split the subjects claim into a list (assuming it's a comma-separated string)
    var subjectList = moderatorSubjects.Split(',');

    // Check if the moderator is authorized to review this report (Updated Line 87)
    if (subjectList == null || !subjectList.Contains(report.Subject ?? string.Empty, StringComparer.OrdinalIgnoreCase))
    {
       return StatusCode(403, new { message = "You are not authorized to review this report." });

    }

    // Proceed with reviewing the report
    report.Status = reviewDto.Approve;
    report.ModeratorComments = reviewDto.ModeratorComments;
    report.DateReviewed = DateTime.UtcNow;

    return Ok(new { message = "Report reviewed successfully.", report });
}

        // GET: api/reporting/search
        [HttpGet("search")]
        public IActionResult SearchReports([FromQuery] string documentId, [FromQuery] string userId, [FromQuery] string reason, [FromQuery] bool? status)
        {
            var filteredReports = ReportDatabase.AsQueryable();

            if (!string.IsNullOrEmpty(documentId))
                filteredReports = filteredReports.Where(r => r.DocumentId.Equals(documentId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(userId))
                filteredReports = filteredReports.Where(r => r.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(reason))
                filteredReports = filteredReports.Where(r => r.Reason.Contains(reason, StringComparison.OrdinalIgnoreCase));
            if (status.HasValue)
                filteredReports = filteredReports.Where(r => r.Status == status.Value);

            return Ok(filteredReports.ToList());
        }

        // GET: api/reporting/file/{documentId}
        [HttpGet("file/{documentId}")]
        public IActionResult GetFile(string documentId)
        {
            var filePath = Path.Combine("path/to/files", documentId); // Adjust this path as necessary

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { message = "File not found." });
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/octet-stream", documentId);
        }
    }

}*/