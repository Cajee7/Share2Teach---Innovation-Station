namespace Search.Models
{
    /// <summary>
    /// Represents a search request containing the user's query.
    /// </summary>
    public class SearchRequest
    {
        /// <summary>
        /// Gets or sets the search query.
        /// </summary>
        public required string Query { get; set; }
    }
}
