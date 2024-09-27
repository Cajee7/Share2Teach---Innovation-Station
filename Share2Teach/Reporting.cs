using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ReportManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportingController : ControllerBase
    {
        private readonly IMongoCollection<ReportDto> _reportCollection;

        public ReportingController(IMongoDatabase database)
        {
            _reportCollection = database.GetCollection<ReportDto>("Reports");
        }

        // POST: api/reporting
        [HttpPost("CreateReport")]
        public async Task<IActionResult> SubmitReport([FromBody] CreateReportDto newReport)
        {
            if (newReport == null || string.IsNullOrEmpty(newReport.DocumentId) || string.IsNullOrEmpty(newReport.Reason))
                return BadRequest(new { message = "Please provide all required information (DocumentId and Reason)." });

            var report = new ReportDto
            {
                Id = ObjectId.GenerateNewId().ToString(),
                DocumentId = newReport.DocumentId,
                Reason = newReport.Reason,
                Status = "pending",  // Set initial status to pending
                DateReported = DateTime.UtcNow
            };

            try
            {
                await _reportCollection.InsertOneAsync(report);
                return CreatedAtAction(nameof(GetAllReports), new { id = report.Id }, report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error saving report to the database.", error = ex.Message });
            }
        }

        // GET: api/reporting
        [HttpGet("GetAllReports")]
        public async Task<IActionResult> GetAllReports()
        {
            var reports = await _reportCollection.Find(r => true).ToListAsync();
            return Ok(reports);
        }

        // PUT: api/reporting/update/{id}
        [HttpPut("updateStatus/{id}")]
        public async Task<IActionResult> UpdateReportStatus(string id, [FromBody] UpdateReportDto updateDto)
        {
            if (updateDto == null)
                return BadRequest(new { message = "Please provide a status to update." });

            // If the status is empty or null, we clear it by setting it to "pending"
            if (string.IsNullOrEmpty(updateDto.Status))
            {
                updateDto.Status = "pending"; // Define what clearing means (set to pending)
            }
            else
            {
                var validStatuses = new[] { "approved", "denied", "pending" }; // Include pending if you want to set it back to this status
                if (!validStatuses.Contains(updateDto.Status.ToLower()))
                    return BadRequest(new { message = "Status must be either 'approved', 'denied', or 'pending'." });
            }

            // Update the status using a case-insensitive comparison
            var update = Builders<ReportDto>.Update.Set(r => r.Status, updateDto.Status);
            var result = await _reportCollection.UpdateOneAsync(
                r => r.Id == id,
                update);

            if (result.ModifiedCount == 0)
                return NotFound(new { message = "Report not found or status unchanged." });

            return Ok(new { message = "Report status updated successfully." });
        }

        // DELETE: api/reporting
        [HttpDelete("DeleteApprovedReports")]
        public async Task<IActionResult> DeleteApprovedReports()
        {
            // Attempt to delete all reports that have the status "approved" (case insensitive)
            var result = await _reportCollection.DeleteManyAsync(r => r.Status.ToLower() == "approved");

            if (result.DeletedCount > 0)
            {
                return Ok(new { message = $"{result.DeletedCount} approved reports deleted." });
            }
            else
            {
                return NotFound(new { message = "No approved reports found to delete." });
            }
        }
    }
}
