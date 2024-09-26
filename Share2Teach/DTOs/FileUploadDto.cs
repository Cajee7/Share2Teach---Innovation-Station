namespace Document_Model.DTOs
{
    public class FileDownloadDto
    {
        public IFormFile File { get; set; }
        public string Title { get; set; }
        public string Subject { get; set; }
        public string Grade { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}