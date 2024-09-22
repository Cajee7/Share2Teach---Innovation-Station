using MongoDB.Bson;
using MongoDB.Driver;
using System;

namespace FAQPage
{
    public class FAQPage
    {
        private static IMongoCollection<BsonDocument> GetFAQCollection()
        {
            var database = DatabaseConnection.Program.ConnectToDatabase();
            return database.GetCollection<BsonDocument>("FAQS");
        }

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

        public static void CreateFAQ(string question, string answer)
        {
            if (string.IsNullOrEmpty(question) || string.IsNullOrEmpty(answer))
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

        public static void UpdateFAQ(string question, string newQuestion, string newAnswer)
        {
            var faqCollection = GetFAQCollection();
            var filter = Builders<BsonDocument>.Filter.Eq("Question", question);

            var update = Builders<BsonDocument>.Update
                .Set("Question", string.IsNullOrEmpty(newQuestion) ? question : newQuestion)
                .Set("answer", string.IsNullOrEmpty(newAnswer) ? "" : newAnswer)
                .Set("dateUpdated", DateTime.UtcNow);

            var result = faqCollection.UpdateOne(filter, update);

            if (result.MatchedCount > 0)
            {
                Console.WriteLine("FAQ updated successfully.");
            }
            else
            {
                Console.WriteLine("FAQ not updated.");
            }
        }

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
            FAQPage.RetrieveFAQS();
        }
    }
}