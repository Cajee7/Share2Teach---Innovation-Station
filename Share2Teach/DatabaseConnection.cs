using MongoDB.Driver;
using MongoDB.Bson;
using System;

namespace DatabaseConnection
{
    /// <summary>
    /// Provides methods to connect to a MongoDB database.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The connection string for the MongoDB database.
        /// </summary>
        private const string connectionString = "mongodb+srv://muhammedcajee29:RU2AtjQc0d8ozPdD@share2teach.vtehmr8.mongodb.net/";

        /// <summary>
        /// The name of the MongoDB database to use.
        /// </summary>
        private const string databasename = "Share2Teach";

        /// <summary>
        /// Connects to the MongoDB database using the specified connection string and database name.
        /// </summary>
        /// <returns>
        /// An instance of <see cref="IMongoDatabase"/> representing the connected database.
        /// Returns null if the connection fails.
        /// </returns>
        public static IMongoDatabase? ConnectToDatabase()
        {
            try
            {
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase(databasename);

                Console.WriteLine("Successfully connected to database");
                return database;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error! Could not connect to database: " + ex.Message);
                return null; // Returns null if connection fails
            }
        }
    }
}
