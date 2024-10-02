using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Document_Model.DTOs
{
    /// <summary>
    /// Data transfer object for downloading a file.
    /// </summary>
    public class FileDownloadDto
    {
        /// <summary>
        /// Gets or sets the file to be downloaded.
        /// </summary>
        public required IFormFile File { get; set; }

        /// <summary>
        /// Gets or sets the title of the file.
        /// </summary>
        public required string Title { get; set; }

        /// <summary>
        /// Gets or sets the subject of the file.
        /// </summary>
        public required string Subject { get; set; }

        /// <summary>
        /// Gets or sets the grade level associated with the file.
        /// </summary>
        public required string Grade { get; set; }

        /// <summary>
        /// Gets or sets the description of the file.
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Gets or sets the tags associated with the file.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
}
