using System;


namespace Share2Teach.Models
{
    public partial class Moderation
    {

        public required int Moderator_id { get; set; }
        public required int User_id { get; set; }
        public required int Document_id { get; set; }
        public required DateTime Date { get; set; }
        public required string Comments { get; set; }
        public int? Ratings { get; set; }
    }
}
