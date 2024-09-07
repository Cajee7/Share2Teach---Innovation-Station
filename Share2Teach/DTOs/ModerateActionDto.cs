namespace DatabaseConnection.DTOs
{
    public class ModerationActionDto
    {
        public ModerationStatus Status { get; set; }  // Enum for status (Approve/Deny)
        public string Comments { get; set; }  // Comments provided by the moderator
        public int Rating { get; set; }  // Rating provided by the moderator (e.g., 1 to 5)
    }
}


