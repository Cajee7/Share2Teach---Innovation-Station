using MongoDB.Bson;
using MongoDB.Driver;
using System;

namespace FAQPage
{
    /*code a button to add questions and answers,
    only available to admins or whoever else needed */
    public class FAQPage
    {
        //method to connect to database and get collection
        private static IMongoCollection<BsonDocument>GetFAQCollection()
        {
            //connecting to database and getting faqs table
            var database = DatabaseConnection.Program.ConnectToDatabase();
            return database.GetCollection<BsonDocument>("FAQS");
        }

        //method to get FAQS from database
        public static void RetrieveFAQS()
        {
            var faqCollection = GetFAQCollection();
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

        //Admin buttons

        //Method to create new FAQ question and answer
        public static void CreateFAQ(string question, string answer)
        {
            if(string.IsNullOrEmpty(question) || string.IsNullOrEmpty(answer))
            {
                Console.WriteLine("Question and answer cannot be empty.");
                return;
            }
            else
            {
                var faqCollection = GetFAQCollection();
                var faqDocument = new BsonDocument
                {
                    { "Question", question },
                    { "answer", answer },
                    { "dateAdded", DateTime.UtcNow }
                };

                faqCollection.InsertOne(faqDocument);
                Console.WriteLine("FAQ added successfully. Good job!");
            }
        }

        //Method to update existing FAQs
        public static void UpdateFAQ(string question, string newQuestion, string newAnswer)
        {
            var faqCollection = GetFAQCollection();
            var filter = Builders<BsonDocument>.Filter.Eq("Question", question);

            var update = Builders<BsonDocument>.Update
                .Set("Question", string.IsNullOrEmpty(newQuestion) ? question : newQuestion) // If new question is not provided, keep old one
                .Set("answer", string.IsNullOrEmpty(newAnswer) ? "" : newAnswer) // Allow empty answer if needed
                .Set("dateUpdated", DateTime.UtcNow); // Add dateUpdated to track changes

            var result = faqCollection.UpdateOne(filter, update);

            if (result.MatchedCount > 0)
            {
                Console.WriteLine("Faq updated successfully");
            } 
            else
            {
                Console.WriteLine("FAQ not updated");
            }
        }

        //Method to delete a FAQ
        public static void DeleteFAQ(string question)
        {

            var faqCollection = GetFAQCollection();
            var filter = Builders<BsonDocument>.Filter.Eq("Question", question);
            var result = faqCollection.DeleteOne(filter);

            if (result.DeletedCount > 0)
            {
                Console.WriteLine("FAQ deleted successfully.");
            }
            else
            {
                Console.WriteLine("FAQ with the given question not found.");
            }    
        }
        public static void Main(string[] args)
        {
            //calling method
            RetrieveFAQS();
        }
    }
}