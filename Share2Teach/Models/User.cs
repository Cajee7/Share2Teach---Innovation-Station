using System;


namespace Share2Teach.Models
{
    public partial class User
    {

        public required int User_id { get; set; }
        public string? FName { get; set; }
        public string? LName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Role { get; set; }
        public required string Subject { get; set; }
    }
}
