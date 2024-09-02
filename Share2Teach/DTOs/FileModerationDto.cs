public class FileModerationDto
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Subject { get; set; }
        public bool? IsApproved { get; set; }  // Nullable to indicate pending approval
        public string ModeratorComments { get; set; }
    }

