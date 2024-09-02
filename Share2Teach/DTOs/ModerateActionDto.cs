//using DatabaseConnection.Models; // Import the namespace where the enum is defined

namespace DatabaseConnection.DTOs
{
    public class ModerationActionDto
    {
        public ModerationStatus Status { get; set; }  // Enum for status (Approve/Deny)
        public string Comments { get; set; }  // Comments provided by the moderator
    }
}