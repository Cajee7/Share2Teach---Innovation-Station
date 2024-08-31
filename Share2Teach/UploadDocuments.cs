using MongoDB.Driver;
using MongoDB.Bson;
using System;
using DatabaseConnection;

namespace Documents
{
    public class Documents
    {
        //setting max file size to 25mbs
        private const long MaxFileSize = 25 * 1024 * 1024;

        //method to upload document and check file size
        public static void UploadDocument(string filePath)
        {
            if(File.Exists(filePath))
            {
                FileInfo fileInfo = new FileInfo(filePath);

                //checking file size
                if(fileInfo.Length <= MaxFileSize)
                {
                    //Remove
                    //Giving file size
                    Console.WriteLine("File within size limit.");

                    //Getting user input
                    Console.WriteLine("Enter the following data: ");
                    

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
            //connecting to database
            var database = DatabaseConnection.Program.ConnectToDatabase();

            if(database != null)
            {
                //lines for testing purposes
                //To be removed
                Console.WriteLine("Success! Document page connected to database");

                //Api needed but for time being using file path
                //Remove
                string filePath = @"";

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