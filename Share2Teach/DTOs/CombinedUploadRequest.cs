using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a combined upload request, containing the uploaded file and its metadata.
/// </summary>
public class CombinedUploadRequest
{
    /// <summary>
    /// Gets or sets the uploaded file.
    /// </summary>
    public IFormFile? UploadedFile { get; set; }

    /// <summary>
    /// Gets or sets the title of the file.
    /// </summary>
    /// <remarks>
    /// The title is required and must be between 2 and 30 characters.
    /// </remarks>
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 30 characters.")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the subject of the file.
    /// </summary>
    /// <remarks>
    /// The subject is required and must be between 2 and 15 characters.
    /// </remarks>
    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(15, MinimumLength = 2, ErrorMessage = "Subject must be between 2 and 15 characters.")]
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the grade level associated with the file.
    /// </summary>
    /// <remarks>
    /// The grade is required.
    /// </remarks>
    [Required(ErrorMessage = "Grade is required.")]
    public int Grade { get; set; }

    /// <summary>
    /// Gets or sets the description of the file.
    /// </summary>
    /// <remarks>
    /// The description is required and must be between 2 and 200 characters.
    /// </remarks>
    [Required(ErrorMessage = "Description is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Description must be between 2 and 200 characters.")]
    public string? Description { get; set; }
}
