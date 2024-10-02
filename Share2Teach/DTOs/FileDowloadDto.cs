using Microsoft.AspNetCore.Http;

/// <summary>
/// Data transfer object for uploading a file.
/// </summary>
public class FileUploadDto
{
    /// <summary>
    /// Gets or sets the file to be uploaded.
    /// This field is required.
    /// </summary>
    public required IFormFile File { get; set; }
}
