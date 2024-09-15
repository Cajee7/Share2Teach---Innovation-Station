using MongoDB.Bson;
using MongoDB.Driver;
using System;

namespace FAQPage
{
    /*code a button to add questions and answers,
    only available to admins or whoever else needed */
    public class FAQPage
    {
        //method to get FAQS from database
        public static void RetrieveFAQS()
        {
            //connecting to database and getting faqs table
            var database = DatabaseConnection.Program.ConnectToDatabase();
            var faqCollection = database.GetCollection<BsonDocument>("FAQS");

            var faqs = faqCollection.Find(new BsonDocument()).ToList();

            if (faqs != null && faqs.Count > 0)
            {
                Console.WriteLine("Frequently Asked Questions: ");

                for (int i = 0; i < faqs.Count; i++)
                {
                    var faq = faqs[i];
                    Console.WriteLine($"Question: {faq["Question"]}");
                    Console.WriteLine($"Answer: {faq["answer"]}");
                    Console.WriteLine($"Date Added: {faq["Date Added"].ToLocalTime()}");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No Frequently Asked Questions Found!");
            }
        }


        public static void Main(string[] args)
        {
            //calling method
            RetrieveFAQS();
        }
    }
}