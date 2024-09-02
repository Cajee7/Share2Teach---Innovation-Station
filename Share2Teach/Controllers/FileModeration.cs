using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseConnection.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileModerationController : ControllerBase
    {
        // This is just a placeholder for storing file data. Replace this with your actual data store or service.
        private static readonly Dictionary<int, FileModerationDto> FilesDatabase = new Dictionary<int, FileModerationDto>();

        // POST: api/filemoderation/approve/{id}
        [HttpPost("approve/{id}")]
        public async Task<IActionResult> ApproveFile(int id, [FromBody] ModerationCommentDto commentDto)
        {
            if (!FilesDatabase.ContainsKey(id))
                return NotFound(new { message = "File not found." });

            var file = FilesDatabase[id];
            file.IsApproved = true;
            file.ModeratorComments = commentDto.Comments;

            // Here you would typically update your data store with the new approval status.
            // For example: _fileService.Update(file);

            return Ok(new { message = "File approved successfully.", file });
        }

        // POST: api/filemoderation/deny/{id}
        [HttpPost("deny/{id}")]
        public async Task<IActionResult> DenyFile(int id, [FromBody] ModerationCommentDto commentDto)
        {
            if (!FilesDatabase.ContainsKey(id))
                return NotFound(new { message = "File not found." });

            var file = FilesDatabase[id];
            file.IsApproved = false;
            file.ModeratorComments = commentDto.Comments;

            // Update your data store with the new disapproval status.
            // For example: _fileService.Update(file);

            return Ok(new { message = "File disapproved successfully.", file });
        }
    }

    // DTO for File Moderation
    public class FileModerationDto
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public bool IsApproved { get; set; }
        public string ModeratorComments { get; set; }
    }

    // DTO for Moderation Comments
    public class ModerationCommentDto
    {
        public string Comments { get; set; }
    }
}
