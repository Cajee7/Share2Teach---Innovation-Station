using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseConnection.DTOs; // Ensure this matches the namespace of ModerationStatus

namespace DatabaseConnection.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileModerationController : ControllerBase
    {
        // Placeholder for storing file data. Replace with your actual data store or service.
        private static readonly List<FileModerationDto> FilesDatabase = new List<FileModerationDto>();

        // POST: api/filemoderation/moderate/{id}
        [HttpPost("moderate/{id}")]
        public async Task<IActionResult> ModerateFile(int id, [FromBody] ModerationActionDto actionDto)
        {
            var file = FilesDatabase.FirstOrDefault(f => f.Id == id);
            if (file == null)
                return NotFound(new { message = "File not found." });

            // Check if file has already been moderated
            if (file.IsApproved.HasValue)
                return BadRequest(new { message = "This file has already been moderated." });

            // Update file moderation status based on the actionDto
            file.IsApproved = actionDto.Status == ModerationStatus.Approve;
            file.ModeratorComments = actionDto.Comments;

            // Update your data store with the new moderation result
            // For example: await _fileService.Update(file);

            var resultMessage = file.IsApproved == true ? "File approved successfully." : "File denied successfully.";
            return Ok(new { message = resultMessage, file });
        }
    }
}
