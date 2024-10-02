using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


public class CombinedUploadRequest
{
    public IFormFile UploadedFile { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 15 characters.")]
    public string Title { get; set; }

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(15, MinimumLength = 2, ErrorMessage = "Subject must be between 2 and 15 characters.")]
    public string Subject { get; set; }

    [Required(ErrorMessage = "Grade is required.")]
    public int Grade { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Description must be between 2 and 250 characters.")]
    public string Description { get; set; }
}
