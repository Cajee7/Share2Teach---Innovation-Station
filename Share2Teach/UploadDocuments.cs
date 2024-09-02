using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.IO; // For file operations
using DatabaseConnection;
using Document_Model.Models; // Referencing models to use model data
using System.Collections.Generic;

namespace Documents
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
                if (fileInfo.Length <= MaxFileSize && fileInfo.Length > 0)
                {
                    Console.WriteLine("File within size limit.");

                    // Path for the converted PDF
                    string outputPdfPath = Path.ChangeExtension(filePath, ".pdf");

                    // Convert the document to PDF
                    ConvertToPdf(filePath, outputPdfPath);

                    // Show new file size
                    FileInfo pdfFileInfo = new FileInfo(outputPdfPath);
                    Console.WriteLine($"Converted PDF Size: {pdfFileInfo.Length} bytes");

                    // Collecting user input
                    var document = new Document_Model.Models.Documents
                    {
                        // Entering initial and automatic values
                        FileSize = pdfFileInfo.Length,
                        DateUploaded = DateTime.UtcNow,
                        ModerationStatus = "Unmoderated",
                        Ratings = 0,
                        Tags = GenerateTags(outputPdfPath) // Method to generate tags
                    };

                    // Getting user input and ensuring no nulls are entered
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

                    // Save the document to the database
                    SaveDocumentToDatabase(document);
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

        // Method to convert to PDFs
        public static void ConvertToPdf(string filePath, string outputPdfPath)
        {
            string fileExtension = Path.GetExtension(filePath).ToLower();

            // Checking file type and converting accordingly
            if (fileExtension == ".doc" || fileExtension == ".docx")
            {
                // Convert Word to PDF
                var wordDocument = new Aspose.Words.Document(filePath);
                wordDocument.Save(outputPdfPath, Aspose.Words.SaveFormat.Pdf);
            }
            else if (fileExtension == ".ppt" || fileExtension == ".pptx")
            {
                // Convert PowerPoint to PDF
                var presentation = new Aspose.Slides.Presentation(filePath);
                presentation.Save(outputPdfPath, Aspose.Slides.Export.SaveFormat.Pdf);
            }
            else
            {
                throw new NotSupportedException("Error! Unsupported file format");
            }
        }

        // Method to generate tags from file content
        private static List<string> GenerateTags(string filePath)
        {
            var tags = new List<string>();

            try
            {
                // Reading file content
                var fileContent = File.ReadAllText(filePath);
                var words = fileContent.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Tagging unique words
                foreach (var word in words)
                {
                    if (word.Length > 3) // Filter short words
                    {
                        tags.Add(word);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error generating tags: " + ex.Message);
            }

            return tags;
        }

        // Method to save the document to the database
        private static void SaveDocumentToDatabase(Document_Model.Models.Documents document)
        {
            // Connect to database
            var database = DatabaseConnection.Program.ConnectToDatabase();

            if (database != null)
            {
                var collection = database.GetCollection<Document_Model.Models.Documents>("Documents");
                collection.InsertOne(document);
                Console.WriteLine("Document saved to database successfully.");
            }
            else
            {
                Console.WriteLine("Failed to connect to the database. Document not saved.");
            }
        }

        public static void Main(string[] args)
        {
            // Example file path (update as needed)
            string filePath = @"path\to\your\file";
            UploadDocument(filePath);
        }
    }
}
