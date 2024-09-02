using System;

namespace Document_Model.Models
{
    public class Documents
    {
        //Database info required
        public string Title { get; set; }
        public string Subject { get; set; }
        public int Grade { get; set; }
        public string Description { get; set; }
        public string ModerationStatus { get; set; } 
        public DateTime DateUploaded { get; set; } 
        public int Ratings { get; set; }
        public List<string> Tags { get; set; } 
        public double FileSize { get; set; } 
    }
}
