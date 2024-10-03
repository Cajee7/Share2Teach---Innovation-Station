/// <summary>
/// Data Transfer Object (DTO) for updating a document in the Share2Teach system.
/// </summary>
public class UpdateDocumentDto
{
    /// <summary>
    /// Gets or sets the title of the document.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the subject of the document.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the grade level associated with the document.
    /// </summary>
    public int Grade { get; set; }

    /// <summary>
    /// Gets or sets a brief description of the document.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the rating of the document.
    /// </summary>
    public int Ratings { get; set; }
}
