using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReportManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportingController : ControllerBase
    {
        private readonly IMongoCollection<ReportDto> _reportCollection;

        public ReportingController()
        {
            var database = DatabaseConnection.Program.ConnectToDatabase(); // Use your custom connection method
            _reportCollection = database.GetCollection<ReportDto>("Reports"); // Change to your collection name
        }

        // GET: api/reporting
        [HttpGet]
        public async Task<IActionResult> GetAllReports()
        {
            var reports = await _reportCollection.Find(r => true).ToListAsync();
            return Ok(reports);
        }

        // POST: api/reporting
        [HttpPost]
        public async Task<IActionResult> SubmitReport([FromBody] CreateReportDto newReport)
        {
            if (newReport == null || string.IsNullOrEmpty(newReport.DocumentId) || string.IsNullOrEmpty(newReport.Reason))
                return BadRequest(new { message = "Please provide all required information (DocumentId and Reason)." });

            var report = new ReportDto
            {
                Id = ObjectId.GenerateNewId().ToString(), // Generate a new ObjectId
                DocumentId = newReport.DocumentId,
                Reason = newReport.Reason,
                Status = "pending",
                DateReported = DateTime.UtcNow
            };

            try
            {
                await _reportCollection.InsertOneAsync(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error saving report to the database.", error = ex.Message });
            }

            return CreatedAtAction(nameof(GetAllReports), new { id = report.Id }, report);
        }

        // DELETE: api/reporting
        [HttpDelete]
        public async Task<IActionResult> DeleteApprovedReports()
        {
            var result = await _reportCollection.DeleteManyAsync(r => r.Status == "approved");
            return Ok(new { message = $"{result.DeletedCount} approved reports deleted." });
        }

        // PUT: api/reporting/update/{id}
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateReportStatus(string id, [FromBody] UpdateReportDto updateDto)
        {
            if (updateDto == null || string.IsNullOrEmpty(updateDto.Status))
                return BadRequest(new { message = "Please provide a status to update." });

            var update = Builders<ReportDto>.Update.Set(r => r.Status, updateDto.Status);
            var result = await _reportCollection.UpdateOneAsync(r => r.Id == id, update);

            if (result.ModifiedCount == 0)
                return NotFound(new { message = "Report not found or status unchanged." });

            return Ok(new { message = "Report status updated successfully." });
        }
    }

}
