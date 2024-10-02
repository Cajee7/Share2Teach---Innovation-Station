using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Document_Model.Models
{
    /// <summary>
    /// Represents a document in the system, containing metadata and file information.
    /// </summary>
    public class Documents
    {
        /// <summary>
        /// Gets or sets the MongoDB ID of the document.
        /// </summary>
        [BsonId]
        public ObjectId Id { get; set; } // MongoDB ID

        /// <summary>
        /// Gets or sets the title of the document.
        /// </summary>
        public required string Title { get; set; }

        /// <summary>
        /// Gets or sets the subject of the document.
        /// </summary>
        public required string Subject { get; set; }

        /// <summary>
        /// Gets or sets the grade level associated with the document.
        /// </summary>
        public int Grade { get; set; }

        /// <summary>
        /// Gets or sets the description of the document.
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Gets or sets the file size of the document in bytes.
        /// </summary>
        public double File_Size { get; set; }

        /// <summary>
        /// Gets or sets the URL of the uploaded document.
        /// </summary>
        public required string File_Url { get; set; }

        /// <summary>
        /// Gets or sets the file type of the document (e.g., PDF, Word).
        /// </summary>
        public required string File_Type { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who uploaded the document.
        /// </summary>
        public string? User_id { get; set; }

        /// <summary>
        /// Gets or sets the name of the user who uploaded the document.
        /// </summary>
        public string? User_Name { get; set; }

        /// <summary>
        /// Gets or sets the moderation status of the document (e.g., Unmoderated).
        /// </summary>
        public string Moderation_Status { get; set; } = "Unmoderated";

        /// <summary>
        /// Gets or sets the ratings for the document.
        /// </summary>
        public int Ratings { get; set; }

        /// <summary>
        /// Gets or sets the list of tags associated with the document.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the date when the document was uploaded.
        /// </summary>
        public DateTime Date_Uploaded { get; set; }

        /// <summary>
        /// Gets or sets the date when the document was last updated.
        /// </summary>
        public DateTime? Date_Updated { get; set; }
    }
}