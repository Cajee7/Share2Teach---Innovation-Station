using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace DatabaseConnection.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private const long MaxFileSize = 25 * 1024 * 1024; // 25 MB
        private static readonly List<string> AllowedFileTypes = new List<string>
        {
            "application/pdf", // PDF
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // Word (docx)
            "application/vnd.openxmlformats-officedocument.presentationml.presentation", // PowerPoint (pptx)
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" // Excel (xlsx)
        };

        // POST: api/files/upload
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile(
            [FromForm] IFormFile file,
            [FromForm] string title,
            [FromForm] string description,
            [FromForm] int grade,
            [FromForm] string subject)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(description) || string.IsNullOrEmpty(subject))
            {
                return BadRequest("Required fields are empty.");
            }

            if (!AllowedFileTypes.Contains(file.ContentType))
            {
                return BadRequest("Incorrect file type.");
            }

            if (file.Length > MaxFileSize)
            {
                return BadRequest("File too large.");
            }

            // Generate a unique file name
            string fileName = $"{Guid.NewGuid()}_{file.FileName}";
            string filePath = Path.Combine("Uploads", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Automatically populate fields
            var fileInfo = new
            {
                Title = title,
                Description = description,
                Grade = grade,
                Subject = subject,
                FileSize = file.Length,
                DateUploaded = DateTime.UtcNow,
                ModerationStatus = "Unmoderated",
                InitialFileType = file.ContentType,
                //Tags = GenerateTags(filePath),
                Ratings = 0
            };

            // Here you would save the fileInfo to your database, for example:
            // SaveFileInfoToDatabase(fileInfo);

            return Ok(new { message = "File upload successful.", fileInfo });
        }

        // Method to generate tags from file content
        /*private List<string> GenerateTags(string filePath)
        {
            var tags = new List<string>();

            try
            {
                // Reading file content
                var fileContent = File.ReadAllText(filePath);
                var words = fileContent.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Tagging unique words
                foreach (var word in words)
                {
                    if (word.Length > 3) // Filter short words
                    {
                        tags.Add(word);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error generating tags: " + ex.Message);
            }

            return tags;
        }*/
    }
}