using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.IO; //For file operations
using DatabaseConnection;
using Document_Model.Models;//Referencing models to use model data
using System.Reflection.Metadata; 

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
                if(fileInfo.Length <= MaxFileSize && fileInfo.Length > 0)
                {
                    //Remove
                    //Giving file size
                    Console.WriteLine("File within size limit.");

                    //Collecting user input
                    var document = new Document_Model.Models.Documents
                    {
                        //Entering initial and automatic values
                        FileSize = fileInfo.Length,
                        DateUploaded = DateTime.UtcNow,
                        ModerationStatus = "Unmoderated",
                        Ratings = 0,
                        Tags = GenerateTags(filePath)//method to generate tags
                    };

                    //Getting user input and ensuring no nulls are entered
                    Console.WriteLine("Enter Document Title: ");
                    document.Title = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(document.Title))
                    {
                        Console.WriteLine("Error! Please enter a title!");
                        return;
                    }

                    Console.WriteLine("Enter the Subject:");
                    document.Subject = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(document.Subject))
                    {
                        Console.WriteLine("Error! Please enter a subject!");
                        return;
                    }

                    Console.WriteLine("Enter the Grade:");

                    if (int.TryParse(Console.ReadLine(), out int grade))
                    {
                        document.Grade = grade;
                    }
                    else
                    {
                        Console.WriteLine("Invalid grade input. Please enter a number.");
                        return;
                    }

                    Console.WriteLine("Enter the Description:");
                    document.Description = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(document.Description))
                    {
                        Console.WriteLine("Error! Please enter a description!");
                        return;
                    }
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

        //Method to generate tags from file content
        private static List<string> GenerateTags(string filePath)
        {
            var tags = new List<string>();

            try
            {
                var fileContent = File.ReadAllText(filePath);
                var words = fileContent.Split(new[] {' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                //tagging unique words
                for (int i =0; i<words.Length; i++)
                {
                    string word = words[i];
                    tags.Add(word);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error generating tags: " + ex.Message);
            }
            return tags;
        }

        //Method to convert to pdfs
        public static void ConvertToPdf(string filePath, string outputPdfPath)
        {
            string userFile = Path.GetExtension(filePath).ToLower();

            //checking user file to determine type of file
            if(userFile == ".doc" || userFile == ".docx")
            {
                //Convert Word ot Pdf
                var wordDocument = new Aspose.Words.Document(filePath);
                wordDocument.Save(outputPdfPath, Aspose.Words.SaveFormat.Pdf);
            }
            else if(userFile == ".ppt" || userFile == ".pptx")
            {
                //Converting powerpoint to PDF
                var presentation = new Aspose.Slides.Presentation(filePath);
                presentation.Save(outputPdfPath, Aspose.Slides.Export.SaveFormat.Pdf);
            }
            else
            {
                throw new NotSupportedException("Error! Unsupported file format");
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
                string outputPdfPath = @"";

                //convert file
                ConvertToPdf(filePath, outputPdfPath);

            }
            else
            {
                Console.WriteLine("Document page failed to connect to database!");
                Console.WriteLine("Please refresh page and if the problem persists, try again later!");
            }
        }
    }
}