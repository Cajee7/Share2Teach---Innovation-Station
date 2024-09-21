using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Document_Model.Models;
using Microsoft.Extensions.Logging;

namespace UploadDocumentsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentUploadController : ControllerBase
    {
        private const long MaxFileSize = 25 * 1024 * 1024;
        private const string NextcloudBaseUrl = "http://localhost:8080/remote.php/dav/files/MuhammedCajee29";
        private const string NextcloudUsername = "MuhammedCajee29";
        private const string NextcloudPassword = "Jaedene12!";
        private readonly ILogger<DocumentUploadController> _logger;

        public DocumentUploadController(ILogger<DocumentUploadController> logger)
        {
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] string title, [FromForm] string subject, [FromForm] string description, [FromForm] int grade)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Error: No file uploaded.");
            }

            if (file.Length > MaxFileSize)
            {
                return BadRequest("Error: File size exceeds 25 MB!");
            }

            var filePath = Path.GetTempFileName();
            var outputPdfPath = Path.ChangeExtension(filePath, ".pdf");

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Convert the file to PDF
                ConvertToPdf(filePath, outputPdfPath);

                // Upload the converted PDF to Nextcloud
                string nextcloudUrl = await UploadToNextcloud(outputPdfPath);
                if (string.IsNullOrEmpty(nextcloudUrl))
                {
                    return StatusCode(500, "Error uploading file to Nextcloud.");
                }

                // Create document metadata
                var document = new Documents
                {
                    Title = title,
                    Subject = subject,
                    Description = description,
                    FileSize = new FileInfo(outputPdfPath).Length,
                    FileUrl = nextcloudUrl,
                    DateUploaded = DateTime.UtcNow,
                    ModerationStatus = "Unmoderated",
                    Ratings = 0,
                    Tags = GenerateTags(outputPdfPath),
                    Grade = grade
                };

                // Save document metadata to MongoDB
                SaveDocumentToDatabase(document);

                return Ok(new { Message = "Document uploaded successfully.", FileUrl = nextcloudUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred during file upload: {ex.Message}");
                return StatusCode(500, "An internal server error occurred.");
            }
            finally
            {
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                if (System.IO.File.Exists(outputPdfPath)) System.IO.File.Delete(outputPdfPath);
            }
        }

        // Upload file to Nextcloud using WebDAV
        private static async Task<string> UploadToNextcloud(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string uploadUrl = $"{NextcloudBaseUrl}/{fileName}";

            using (HttpClient client = new HttpClient())
            {
                var byteArray = Encoding.ASCII.GetBytes($"{NextcloudUsername}:{NextcloudPassword}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                using (var content = new StreamContent(System.IO.File.OpenRead(filePath)))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    try
                    {
                        HttpResponseMessage response = await client.PutAsync(uploadUrl, content);
                        if (response.IsSuccessStatusCode)
                        {
                            return uploadUrl;
                        }
                        else
                        {
                            string errorMessage = await response.Content.ReadAsStringAsync();
                            throw new Exception($"Upload failed: {response.StatusCode} - {errorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error during file upload: {ex.Message}");
                    }
                }
            }
        }

        // Convert file to PDF using LibreOffice
        private static void ConvertToPdf(string filePath, string outputPdfPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "libreoffice",
                Arguments = $"--headless --convert-to pdf \"{filePath}\" --outdir \"{Path.GetDirectoryName(outputPdfPath)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string error = process.StandardError.ReadToEnd();
                    throw new Exception($"LibreOffice conversion failed: {error}");
                }
            }
        }

        // Generate tags from file content
        private static List<string> GenerateTags(string filePath)
        {
            var tags = new List<string>();

            try
            {
                var fileContent = System.IO.File.ReadAllText(filePath);
                var words = fileContent.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in words)
                {
                    if (word.Length > 3)
                    {
                        tags.Add(word);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating tags: " + ex.Message);
            }

            return tags;
        }

        // Save the document to the database
        private static void SaveDocumentToDatabase(Documents document)
        {
            var database = DatabaseConnection.Program.ConnectToDatabase();
            if (database != null)
            {
                var collection = database.GetCollection<Documents>("Documents");
                collection.InsertOne(document);
            }
            else
            {
                throw new Exception("Failed to connect to the database.");
            }
        }
    }
}
