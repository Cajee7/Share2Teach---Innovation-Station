using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBExample
{
    // Define the data model
    public class User
    {
        public ObjectId Id { get; set; } // MongoDB uses ObjectId by default for _id
        public string FName { get; set; }
        public string LName { get; set; }
    }

    // MongoDB service class
    public class MongoDBService
    {
        private readonly IMongoDatabase _database;

        public MongoDBService(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<User> GetUsersCollection()
        {
            return _database.GetCollection<User>("Users");
        }
    }

    // Main program class
    public class Program
    {
        private static async Task Main(string[] args)
        {
            // Connection string and database name
            string connectionString = "mongodb+srv://muhammedcajee29:RU2AtjQc0d8ozPdD@share2teach.vtehmr8.mongodb.net/";
            string databaseName = "share2teach"; // Specify the database name here

            // Initialize MongoDB service
            var mongoDbService = new MongoDBService(connectionString, databaseName);

            // Get the collection
            var usersCollection = mongoDbService.GetUsersCollection();

            // Query to find all documents
            var users = await usersCollection.Find(_ => true).ToListAsync();

            // Display FName and LName
            foreach (var user in users)
            {
                Console.WriteLine($"First Name: {user.FName}, Last Name: {user.LName}");
            }
        }
    }
}
