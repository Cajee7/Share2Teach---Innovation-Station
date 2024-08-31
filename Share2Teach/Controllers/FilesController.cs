using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace YourNamespace.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
        private static readonly List<string> AllowedFileTypes = new List<string> { "application/pdf", "image/jpeg", "image/png" };
        private readonly UserManager<IdentityUser> _userManager;

        public FilesController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // POST: api/files/upload
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile(
            [FromForm] IFormFile file,
            [FromForm] string fileType,
            [FromForm] string fileName,
            [FromForm] string subject,
            [FromForm] string grade,
            [FromForm] DateTime dateCreated)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            if (string.IsNullOrEmpty(fileType) || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(grade))
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

            var filePath = Path.Combine("Uploads", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileInfo = new
            {
                FileName = fileName,
                FilePath = filePath,
                FileType = fileType,
                Subject = subject,
                Grade = grade,
                DateCreated = dateCreated,
                FileSize = file.Length,
                ContentType = file.ContentType
            };

            return Ok(new { message = "File upload successful.", fileInfo });
        }

        // POST: api/files/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new IdentityUser { UserName = model.Username, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
                return Ok(new { message = "User registered successfully" });

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return BadRequest(ModelState);
        }
    }

    // DTO for User Registration
    public class UserRegistrationDto
{
    public required string Username { get; set; } // Required property
    public required string Email { get; set; }    // Required property
    public required string Password { get; set; } // Required property
    public required string ConfirmPassword { get; set; } // Required property
}
}
