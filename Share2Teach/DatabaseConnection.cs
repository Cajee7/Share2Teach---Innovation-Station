using MongoDB.Driver;
using MongoDB.Bson;
using System;

namespace DatabaseConnection
{
    public class Program
    {
        //connection string for databse and name of database to use
        private const string connectionString = "mongodb+srv://muhammedcajee29:RU2AtjQc0d8ozPdD@share2teach.vtehmr8.mongodb.net/";
        private const string databasename = "Share2Teach";

        //method to connect to database
        public static IMongoDatabase ConnectToDatabase()
        {
            try
            {
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase(databasename);

                Console.WriteLine("Succesfully connected to database");
                return database;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error! Could not connect to database: " + ex.Message);
                return null; //returns null if connection fails
            }
            
        }
    }
}