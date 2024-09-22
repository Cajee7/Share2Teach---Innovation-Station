public class ReportReviewDto
    {
        public bool Approve { get; set; } // Indicates whether the report is approved or denied
        public required string ModeratorComments { get; set; } // Comments from the moderator
    }