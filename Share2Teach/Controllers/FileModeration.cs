using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseConnection.DTOs;

namespace DatabaseConnection.Controllers
{
    //Api controller to handle HTTP rewuests and returns HTTP responses 
    [ApiController]
    [Route("api/[controller]")]//Name of the route like a placeholder
    public class FileModerationController : ControllerBase
    {
        // Replace this with a call to the database
        private static readonly List<FileModerationDto> FilesDatabase = new List<FileModerationDto>
        {
            new FileModerationDto { Id = 1, FileName = "file1.txt", FilePath = "/files/file1.txt", Subject = "Math", IsApproved = null },
            new FileModerationDto { Id = 2, FileName = "file2.txt", FilePath = "/files/file2.txt", Subject = "Science", IsApproved = null },
            new FileModerationDto { Id = 3, FileName = "file3.txt", FilePath = "/files/file3.txt", Subject = "Math", IsApproved = true }
        };

        //Returns a list of files that havent been moderated yet
        [HttpGet("unmoderated")]
        public IActionResult GetUnmoderatedFiles()
        {
            // Assuming the user's subjects are available from authentication/authorization layer.
            var userSubjects = GetUserSubjects(); // Mock method to get user subjects

            // Filter the files based on whether they are unmoderated and the subjects the moderator teaches.
            var unmoderatedFiles = FilesDatabase
                .Where(f => f.IsApproved == null && userSubjects.Contains(f.Subject))
                .ToList();

            return Ok(unmoderatedFiles);
        }

        //Allows the moderator to approve or deny a file
        [HttpPost("moderate/{id}")]
        public async Task<IActionResult> ModerateFile(int id, [FromBody] ModerationActionDto actionDto)
        {
            var file = FilesDatabase.FirstOrDefault(f => f.Id == id);
            if (file == null)
                return NotFound(new { message = "File not found." });

            // Checks if file has already been moderated
            if (file.IsApproved.HasValue)
                return BadRequest(new { message = "This file has already been moderated." });

            // Update file moderation status based on the actionDto
            file.IsApproved = actionDto.Status == ModerationStatus.Approve;
            file.ModeratorComments = actionDto.Comments;
            file.Rating = actionDto.Rating;

            // Simulate database update or some data store update
            var resultMessage = file.IsApproved == true ? "File approved successfully." : "File denied successfully.";
            return Ok(new { message = resultMessage, file });
        }

        // Mock method to get user subjects from authentication/authorization
        private List<string> GetUserSubjects()
        {
            // I need to replace this code with actual user subjects fetching logic
            return new List<string> { "Math", "Science" };  // Example subjects for the moderator
        }
    }
}
