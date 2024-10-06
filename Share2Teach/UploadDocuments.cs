using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.IO;
using System.Diagnostics;
using Document_Model.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DatabaseConnection; // Import database connection class

namespace UploadDocuments
{
    /// <summary>
    /// Provides functionality for uploading documents to Nextcloud.
    /// </summary>
    public class DocumentUploader
    {
        private const long MaxFileSize = 25 * 1024 * 1024; // 25 MB max size
        private const string AllowedFileTypes = ".docx,.pptx,.xlsx,.pdf"; // Allowed file types
        private const string NextcloudBaseUrl = "http://localhost:8080/remote.php/dav/files/aramsunar/";
        private const string NextcloudUsername = "aramsunar";
        private const string NextcloudPassword = "Jaedene12!";

        // Method to handle the full process of document upload
        public static async Task UploadDocument(string filePath)
        {
            // Check if file exists
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Error: File not found!");
                return;
            }

            FileInfo fileInfo = new FileInfo(filePath);

            // Check file size
            if (fileInfo.Length > MaxFileSize || fileInfo.Length <= 0)
            {
                Console.WriteLine("Error: File size exceeds 25 MB or is empty!");
                return;
            }

            // Check file type
            if (!AllowedFileTypes.Contains(fileInfo.Extension.ToLower()))
            {
                Console.WriteLine("Error: Unsupported file type!");
                return;
            }

            Console.WriteLine("File within size and type limits.");

            // Convert to PDF if necessary
            string outputPdfPath = Path.ChangeExtension(filePath, ".pdf");
            ConvertToPdf(filePath, outputPdfPath);

            // Upload to Nextcloud and get the file URL
            string? nextcloudUrl = await UploadToNextcloud(outputPdfPath);

            if (string.IsNullOrEmpty(nextcloudUrl))
            {
                Console.WriteLine("Error uploading file to Nextcloud.");
                return; 
            }

            // Initialize document metadata
            var document = CollectDocumentDetails(outputPdfPath, nextcloudUrl);

            // Save the document to MongoDB
            SaveDocumentToDatabase(document);
        }

        // Method to upload a file to Nextcloud using WebDAV
        private static async Task<string?> UploadToNextcloud(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string uploadUrl = $"{NextcloudBaseUrl}/{fileName}";

            using (HttpClient client = new HttpClient())
            {
                var byteArray = Encoding.ASCII.GetBytes($"{NextcloudUsername}:{NextcloudPassword}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                using (var content = new StreamContent(File.OpenRead(filePath)))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    try
                    {
                        HttpResponseMessage response = await client.PutAsync(uploadUrl, content);
                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("File uploaded successfully to Nextcloud.");
                            return uploadUrl;
                        }
                        else
                        {
                            string errorMessage = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Upload failed: {response.StatusCode} - {errorMessage}");
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during file upload: {ex.Message}");
                        return null;
                    }
                }
            }
        }

        // Method to convert documents to PDFs using LibreOffice
        private static void ConvertToPdf(string filePath, string outputPdfPath)
        {
            if (Path.GetExtension(filePath).ToLower() == ".pdf")
            {
                // If it's already a PDF, skip the conversion
                Console.WriteLine("File is already in PDF format.");
                return;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "libreoffice",
                    Arguments = $"--headless --convert-to pdf \"{filePath}\" --outdir \"{Path.GetDirectoryName(outputPdfPath)}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        string error = process.StandardError.ReadToEnd();
                        throw new Exception($"LibreOffice conversion failed: {error}");
                    }
                }

                Console.WriteLine("File successfully converted to PDF.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during PDF conversion: {ex.Message}");
            }
        }

        // Prompt user to input required document details
        private static Documents CollectDocumentDetails(string filePath, string fileUrl)
        {
            FileInfo pdfFileInfo = new FileInfo(filePath);

            var document = new Document_Model.Models.Documents
            {
                Title = "Placeholder Title",
                Subject = "Placeholder Subject",
                Description = "Placeholder Desc.",
                File_Size = pdfFileInfo.Length,
                File_Url = fileUrl,
                File_Type = pdfFileInfo.Extension,
                Date_Uploaded = DateTime.UtcNow,
                Moderation_Status = "Unmoderated",
                Ratings = 0,
                Tags = GenerateTags(filePath)
            };

            // Collect user input
            Console.WriteLine("Enter Document Title: ");
            string? titleInput = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(titleInput))
            {
                Console.WriteLine("Error! Please enter a valid title!");
                throw new Exception("Title is required.");
            }
            document.Title = titleInput;

            Console.WriteLine("Enter the Subject:");
            string? subjectInput = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(subjectInput))
            {
                Console.WriteLine("Error! Please enter a valid subject!");
                throw new Exception("Subject is required.");
            }
            document.Subject = subjectInput;

            Console.WriteLine("Enter the Grade:");
            if (!int.TryParse(Console.ReadLine(), out int grade))
            {
                Console.WriteLine("Invalid grade input. Please enter a number.");
                throw new Exception("Valid grade is required.");
            }
            document.Grade = grade;

            Console.WriteLine("Enter the Description:");
            string? descriptionInput = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(descriptionInput))
            {
                Console.WriteLine("Error! Please enter a valid description!");
                throw new Exception("Description is required.");
            }
            document.Description = descriptionInput;

            return document;
        }

        // Generate tags from the document content
        private static List<string> GenerateTags(string filePath)
        {
            var tags = new List<string>();

            try
            {
                var fileContent = File.ReadAllText(filePath);
                var words = fileContent.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in words)
                {
                    if (word.Length > 3)
                    {
                        tags.Add(word);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error generating tags: " + ex.Message);
            }
             Console.WriteLine($"Generated Tags: {string.Join(", ", tags)}");
            return tags;
        }

        // Save document metadata to MongoDB
        private static void SaveDocumentToDatabase(Documents document)
        {
            var database = DatabaseConnection.Program.ConnectToDatabase();

            if (database != null)
            {
                var collection = database.GetCollection<Documents>("Documents");
                collection.InsertOne(document);
                Console.WriteLine("Document saved to database successfully.");
            }
            else
            {
                Console.WriteLine("Failed to connect to the database.");
            }
        }

        public static void Main(string[] args)
        {
            string filePath = @"path\to\your\file";
            Task.Run(() => UploadDocument(filePath)).Wait();
        }
    }
}