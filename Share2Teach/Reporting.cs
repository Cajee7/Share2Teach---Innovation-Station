using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportingController : ControllerBase
    {
        // Helper method to get the MongoDB Reports collection
        private static IMongoCollection<BsonDocument> GetReportCollection()
        {
            var database = DatabaseConnection.Program.ConnectToDatabase();
            return database.GetCollection<BsonDocument>("Reports");
        }

       [HttpPost("save-report")]
public IActionResult SaveReport([FromBody] SaveReportDto saveReportDto)
{
    if (saveReportDto == null || string.IsNullOrEmpty(saveReportDto.DocumentId) ||
        string.IsNullOrEmpty(saveReportDto.Reason))
    {
        return BadRequest(new { message = "DocumentId and Reason are required." });
    }

    // Ensure that DocumentId is a valid ObjectId string
    if (!ObjectId.TryParse(saveReportDto.DocumentId, out ObjectId documentObjectId))
    {
        return BadRequest(new { message = "Invalid DocumentId format." });
    }

    var reportCollection = GetReportCollection();

    var reportDocument = new BsonDocument
    {
        { "DocumentId", documentObjectId },
        { "Reason", saveReportDto.Reason },
        { "ReportStatus", saveReportDto.Report_status ?? "Pending" },
        { "DateReported", DateTime.UtcNow }
    };

    reportCollection.InsertOne(reportDocument);

    return CreatedAtAction(nameof(GetReports), new { id = reportDocument["_id"].ToString() }, reportDocument);
}


        // GET: api/reporting/GetReports
        [HttpGet("GetReports")]
        public IActionResult GetReports()
        {
            var reportCollection = GetReportCollection();

            // No filters applied, fetch all reports
            var reports = reportCollection.Find(Builders<BsonDocument>.Filter.Empty).ToList();

            // Ensure that fields are accessed properly, and ObjectId is cast to string
            var formattedReports = reports.Select(report => new {
                Id = report["_id"].AsObjectId.ToString(),  // Ensure correct handling of ObjectId
                DocumentId = report["DocumentId"].AsString,  // DocumentId is treated as a string
                Reason = report["Reason"].AsString,
                Report_Status = report["Report_Status"].AsString,  // Corrected field name
                Date_Reported = report["Date_Reported"].ToUniversalTime()
            }).ToList();

            return Ok(formattedReports);
        }

        // PUT: api/reporting/update-status/{id}
        [HttpPut("update-status/{id}")]
        public IActionResult UpdateReportStatus(string id, [FromBody] UpdateReportStatusDto updateReportStatusDto)
        {
            // Validate and convert the string id to ObjectId
            if (!ObjectId.TryParse(id, out ObjectId reportObjectId))
            {
                return BadRequest(new { message = "Invalid report id format." });
            }

            var reportCollection = GetReportCollection();
            var filter = Builders<BsonDocument>.Filter.Eq("_id", reportObjectId);

            var report = reportCollection.Find(filter).FirstOrDefault();
            if (report == null)
                return NotFound(new { message = "Report not found." });

            // Validate Report_Status
            if (string.IsNullOrEmpty(updateReportStatusDto.Report_Status))
            {
                return BadRequest(new { message = "Report status is required." });
            }

            // Update the Status as a string
            var update = Builders<BsonDocument>.Update
                .Set("Report_Status", updateReportStatusDto.Report_Status)  // Status as string
                .Set("DateReviewed", DateTime.UtcNow);

            var result = reportCollection.UpdateOne(filter, update);

            if (result.MatchedCount > 0)
            {
                return Ok(new { message = "Report status updated successfully." });
            }

            return BadRequest(new { message = "Report status update failed." });
        }

        // DELETE: api/reporting/delete-approved
        [HttpDelete("delete-approved")]
        public IActionResult DeleteApprovedReports()
        {
            var reportCollection = GetReportCollection();

            // Ensure we are filtering for documents where Status is explicitly a string with value "Approved"
            var filter = Builders<BsonDocument>.Filter.Eq("Report_Status", "Approved");

            var deletedReports = reportCollection.Find(filter).ToList();  // Retrieve reports to return them
            var deleteResult = reportCollection.DeleteMany(filter);

            if (deleteResult.DeletedCount > 0)
            {
                return Ok(new { message = $"{deleteResult.DeletedCount} approved reports deleted.", reports = deletedReports });
            }

            return BadRequest(new { message = "No approved reports found to delete." });
        }
    }
}
