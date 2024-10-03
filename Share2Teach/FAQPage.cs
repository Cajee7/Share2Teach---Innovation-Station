using MongoDB.Bson;
using MongoDB.Driver;
using System;

namespace FAQPage
{
    /// <summary>
    /// Represents a page for managing frequently asked questions (FAQs).
    /// </summary>
    public class FAQPage
    {
        /// <summary>
        /// Retrieves the FAQ collection from the database.
        /// </summary>
        /// <returns>The MongoDB collection containing FAQs.</returns>
        private static IMongoCollection<BsonDocument> GetFAQCollection()
        {
            var database = DatabaseConnection.Program.ConnectToDatabase();
            
            // Check if the database connection was successful
            if (database == null)
            {
                throw new InvalidOperationException("Failed to connect to the database.");
            }
            
            return database.GetCollection<BsonDocument>("FAQS");
        }

        /// <summary>
        /// Retrieves and displays all FAQs from the collection.
        /// </summary>
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

        /// <summary>
        /// Creates a new FAQ entry in the collection.
        /// </summary>
        /// <param name="question">The question of the FAQ.</param>
        /// <param name="answer">The answer of the FAQ.</param>
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

        /// <summary>
        /// Updates an existing FAQ entry in the collection.
        /// </summary>
        /// <param name="question">The original question of the FAQ to be updated.</param>
        /// <param name="newQuestion">The new question to replace the original question.</param>
        /// <param name="newAnswer">The new answer to replace the original answer.</param>
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

        /// <summary>
        /// Deletes an existing FAQ entry from the collection.
        /// </summary>
        /// <param name="question">The question of the FAQ to be deleted.</param>
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

        /// <summary>
        /// The main entry point for the application.
        /// Calls the method to retrieve FAQs.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        public static void Main(string[] args)
        {
            //calling method
            FAQPage.RetrieveFAQS();
        }
    }
}
