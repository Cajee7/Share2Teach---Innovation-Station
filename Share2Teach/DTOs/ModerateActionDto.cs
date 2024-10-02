namespace DatabaseConnection.DTOs
{
    /// <summary>
    /// Represents the data transfer object for moderation actions, containing information about the moderation status, comments, and rating.
    /// </summary>
    public class ModerationActionDto
    {
        /// <summary>
        /// Gets or sets the moderation status (e.g., Approve or Deny).
        /// </summary>
        public ModerationStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the comments provided by the moderator regarding the moderation action.
        /// </summary>
        public string Comments { get; set; } = string.Empty;  // Ensures non-null value

        /// <summary>
        /// Gets or sets the rating provided by the moderator (e.g., 1 to 5).
        /// </summary>
        public int Rating { get; set; }
    }
}
