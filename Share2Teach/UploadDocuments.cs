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

namespace UploadDocuments
{
    public class DocumentUploader
    {
        private const long MaxFileSize = 25 * 1024 * 1024;
        private const string NextcloudBaseUrl = "http://localhost:8080/remote.php/dav/files/aramsunar/";
        private const string NextcloudUsername = "aramsunar";
        private const string NextcloudPassword = "Jaedene12!";

        // Method to upload document and check file size
        public static async Task UploadDocument(string filePath)
        {
            if (File.Exists(filePath))
            {
                FileInfo fileInfo = new FileInfo(filePath);

                if (fileInfo.Length <= MaxFileSize && fileInfo.Length > 0)
                {
                    Console.WriteLine("File within size limit.");
                    string outputPdfPath = Path.ChangeExtension(filePath, ".pdf");

                    // Convert to PDF using LibreOffice
                    ConvertToPdf(filePath, outputPdfPath);

                    FileInfo pdfFileInfo = new FileInfo(outputPdfPath);
                    Console.WriteLine($"Converted PDF Size: {pdfFileInfo.Length} bytes");

                    // Upload to Nextcloud and get the file URL
                    string nextcloudUrl = await UploadToNextcloud(outputPdfPath);

                    if (string.IsNullOrEmpty(nextcloudUrl))
                    {
                        Console.WriteLine("Error uploading file to Nextcloud.");
                        return;
                    }

                    // Initialize the document with required fields as empty strings
                    var document = new Document_Model.Models.Documents
                    {
                        Title = string.Empty, // Placeholder for user input
                        Subject = string.Empty, // Placeholder for user input
                        Description = string.Empty, // Placeholder for user input
                        FileSize = pdfFileInfo.Length,
                        FileUrl = nextcloudUrl,
                        DateUploaded = DateTime.UtcNow,
                        ModerationStatus = "Unmoderated",
                        Ratings = 0,
                        Tags = GenerateTags(outputPdfPath)
                    };

                    // Get user input for required fields
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
                    if (!int.TryParse(Console.ReadLine(), out int grade))
                    {
                        Console.WriteLine("Invalid grade input. Please enter a number.");
                        return;
                    }
                    document.Grade = grade;

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

        // Upload file to Nextcloud using WebDAV
        private static async Task<string> UploadToNextcloud(string filePath)
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

        // Convert to PDFs using LibreOffice
        public static void ConvertToPdf(string filePath, string outputPdfPath)
        {
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

        // Generate tags from file content
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

            return tags;
        }

        // Save the document to the database
        private static void SaveDocumentToDatabase(Document_Model.Models.Documents document)
        {
            var database = DatabaseConnection.Program.ConnectToDatabase();

            if (database != null)
            {
                var collection = database.GetCollection<Document_Model.Models.Documents>("Documents");
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
