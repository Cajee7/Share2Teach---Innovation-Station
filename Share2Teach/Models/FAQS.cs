using System;


namespace Share2Teach.Models
{
    public partial class FAQS
    {

        public required int FAQ_id { get; set; }
        public required string Question { get; set; }
        public required string Answer { get; set; }
    }
}
