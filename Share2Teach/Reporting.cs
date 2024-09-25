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

        private static IMongoCollection<BsonDocument> GetReportCollection()
        {
            var database = DatabaseConnection.Program.ConnectToDatabase();
            return database.GetCollection<BsonDocument>("Reports");
        }
        private static readonly List<SaveReportDto> ReportDatabase = new List<SaveReportDto>
        {
            new SaveReportDto {DocumentId = "66d3a09a4ddfb71ae03ccfd2",Reason = "Inappropriate content" },
            new SaveReportDto {DocumentId = "66f1f2140440e3538e5fdb4c",Reason = "Outdated" },
            new SaveReportDto {DocumentId = "66f1f2680440e3538e5fdb4d",Reason = "Inaccurate"}
        };

        // POST: api/reporting/save-report
        [HttpPost("save-report")]
        public IActionResult SaveReport([FromBody] SaveReportDto saveReportDto)
        {
            // Validate input: ensure that DocumentId (as string) and Reason are provided
            if (saveReportDto == null || string.IsNullOrEmpty(saveReportDto.DocumentId) || string.IsNullOrEmpty(saveReportDto.Reason))
            {
                return BadRequest(new { message = "DocumentId and Reason are required." });
            }

            // Validate and convert DocumentId from string to ObjectId
            if (!ObjectId.TryParse(saveReportDto.DocumentId, out ObjectId documentObjectId))
            {
                return BadRequest(new { message = "Invalid DocumentId format." });
            }

            var reportCollection = GetReportCollection();

            // Create the document with ObjectId, Reason, and set Status to "Pending"
            var reportDocument = new BsonDocument
            {
                { "DocumentId", saveReportDto.DocumentId },
                { "Reason", saveReportDto.Reason },
                { "Status", "Pending" },  // Set Status to "Pending" initially
                { "DateReported", DateTime.UtcNow },  // Automatically set the current date and time
                //{ "DateSubmitted", DateTime.UtcNow }
            };

            // Insert the document into the MongoDB collection
            reportCollection.InsertOne(reportDocument);

            return CreatedAtAction(nameof(SaveReport), new { id = reportDocument["_id"].ToString() }, reportDocument);
        }

        // GET: api/reporting/GetReports
        [HttpGet("GetReports")]
        public IActionResult GetReports()
        {
            var reportCollection = GetReportCollection();

            // No filters applied, fetch all reports
            var reports = reportCollection.Find(Builders<BsonDocument>.Filter.Empty).ToList();

            return Ok(reports);
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

            var update = Builders<BsonDocument>.Update
                .Set("Status", updateReportStatusDto.Status)
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
            var filter = Builders<BsonDocument>.Filter.Eq("Status", "Approved");  // Assuming Status is a string

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
