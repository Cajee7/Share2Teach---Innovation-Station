namespace DatabaseConnection.DTOs
{
    /// <summary>
    /// Represents the data transfer object for file moderation, containing information about the file and moderation status.
    /// </summary>
    public class FileModerationDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the file moderation entry.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the file being moderated.
        /// </summary>
        public string FileName { get; set; } = string.Empty;  // Ensures non-null value

        /// <summary>
        /// Gets or sets the file path of the file being moderated.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;  // Ensures non-null value

        /// <summary>
        /// Gets or sets the subject or category of the file.
        /// </summary>
        public string Subject { get; set; } = string.Empty;   // Ensures non-null value

        /// <summary>
        /// Gets or sets the approval status of the file. Nullable to indicate pending approval.
        /// </summary>
        public bool? IsApproved { get; set; }

        /// <summary>
        /// Gets or sets comments from the moderator regarding the file.
        /// </summary>
        public string? ModeratorComments { get; set; }

        /// <summary>
        /// Gets or sets the rating given to the file by the moderator. Nullable to represent no rating.
        /// </summary>
        public int? Rating { get; set; }
    }
}
