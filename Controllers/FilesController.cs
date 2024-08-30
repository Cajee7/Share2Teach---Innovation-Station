using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace DatabaseConnection.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        // GET: api/files
        [HttpGet]
        public ActionResult<IEnumerable<string>> GetFiles()
        {
            return new string[] { "file1.pdf", "file2.docx" };
        }

        // POST: api/files
        [HttpPost]
        public IActionResult UploadFile([FromBody] string file)
        {
            // Code to handle file upload
            return Ok("File uploaded successfully");
        }
    }
}
