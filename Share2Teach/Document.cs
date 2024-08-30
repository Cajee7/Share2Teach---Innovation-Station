using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.IO; // Add this for File-related operations
using DatabaseConnection; // Correct namespace for accessing Program class

namespace Documents // Change namespace to match your class structure
{
    public class Documents
    {
        // Setting max file size to 25MB
        private const long MaxFileSize = 25 * 1024 * 1024;

        // Method to upload document and check file size
        public static void UploadDocument(string filePath)
        {
            if (File.Exists(filePath))
            {
                FileInfo fileInfo = new FileInfo(filePath);

                // Checking file size
                if (fileInfo.Length <= MaxFileSize)
                {
                    Console.WriteLine("File within size limit.");
                }
                else
                {
                    Console.WriteLine("Error: File size exceeds 25 MB!");
                }
            }
            else
            {
                Console.WriteLine("Error: File not found!");
            }
        }
 
        public static void Main(string[] args)
        {
            // Connecting to database
            var database = DatabaseConnection.Program.ConnectToDatabase(); // Referencing the method correctly

            if (database != null)
            {
                Console.WriteLine("Success! Connected to database");

                // Example file path for testing
                string filePath = @"path\to\your\file.txt";

                UploadDocument(filePath);
            }
            else
            {
                Console.WriteLine("Document page failed to connect to database!");
                Console.WriteLine("Please refresh page and if the problem persists, try again later!");
            }
        }
    }
}