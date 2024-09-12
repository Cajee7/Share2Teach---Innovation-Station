using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class FileDownload
{
    static async Task Main(string[] args)
    {
        // WebDAV URL to the file you want to download
        string webdavUrl = "http://localhost:8080/remote.php/dav/files/aramsunar/uploaded-file.txt";
        string username = Environment.GetEnvironmentVariable("WEBDAV_USERNAME") ?? "aramsunar";
        string password = Environment.GetEnvironmentVariable("WEBDAV_PASSWORD") ?? "Jaedene12!";
        
        // Local path where the file will be saved
        string localFilePath = @"C:\path\to\save\file.txt";

        using (HttpClient client = new HttpClient())
        {
            // Basic Authentication
            var byteArray = new System.Text.ASCIIEncoding().GetBytes($"{username}:{password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            try
            {
                // Download file
                HttpResponseMessage response = await client.GetAsync(webdavUrl);

                if (response.IsSuccessStatusCode)
                {
                    byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(localFilePath, fileBytes);
                    Console.WriteLine("File downloaded successfully.");
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
    }
}
