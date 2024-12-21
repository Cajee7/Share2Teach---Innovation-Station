using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace ReportManagement.Controllers
{
    /// <summary>
    /// Handles reporting-related operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ReportingController : ControllerBase
    {
        private readonly IMongoCollection<ReportDto> _reportCollection;
        private readonly ILogger<ReportingController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportingController"/> class.
        /// </summary>
        /// <param name="database">The MongoDB database instance.</param>
        public ReportingController(IMongoDatabase database)
        {
            _reportCollection = database.GetCollection<ReportDto>("Reports");
        }

       [HttpPost("CreateReport")]
        public async Task<IActionResult> SubmitReport([FromBody] CreateReportDto newReport)
        {
            try
            {
                _logger.LogInformation("Received report request: {@ReportData}", 
                    new { DocumentId = newReport?.DocumentId, Reason = newReport?.Reason });

                // Validate the input
                if (newReport == null)
                {
                    return BadRequest(new { message = "Report data is required." });
                }

                if (string.IsNullOrEmpty(newReport.DocumentId) || string.IsNullOrEmpty(newReport.Reason))
                {
                    return BadRequest(new { message = "DocumentId and Reason are required." });
                }

                // Extract the necessary components from the provided ID
                var idComponents = newReport.DocumentId.Split('-');
                string documentId;

                if (idComponents.Length > 1)
                {
                    // If the ID contains hyphens, combine all parts
                    documentId = string.Join("", idComponents);
                    // Ensure it's exactly 24 characters by padding if necessary
                    documentId = documentId.PadRight(24, '0').Substring(0, 24);
                }
                else
                {
                    documentId = newReport.DocumentId.PadRight(24, '0').Substring(0, 24);
                }

                // Create the report
                var report = new ReportDto
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    DocumentId = documentId,
                    Reason = newReport.Reason.Trim(),
                    Status = "pending",
                    DateReported = DateTime.UtcNow
                };

                // Insert into MongoDB
                await _reportCollection.InsertOneAsync(report);
                _logger.LogInformation("Report created successfully with ID: {ReportId}", report.Id);

                return Ok(new { 
                    message = "Report created successfully",
                    id = report.Id 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing report: {@ReportData}", 
                    new { DocumentId = newReport?.DocumentId, Reason = newReport?.Reason });
                
                return StatusCode(500, new { 
                    message = "An error occurred while processing your request.",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Retrieves all reports.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [HttpGet("GetAllReports")]
        public async Task<IActionResult> GetAllReports()
        {
            var reports = await _reportCollection.Find(r => true).ToListAsync();
            return Ok(reports);
        }

        /// <summary>
        /// Updates the status of an existing report.
        /// </summary>
        /// <param name="id">The ID of the report to update.</param>
        /// <param name="updateDto">The new status data for the report.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Deletes all approved reports.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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
