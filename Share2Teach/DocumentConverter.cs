using Aspose.Words;
using Aspose.Slides;
using System;
using System.IO;
using System.Runtime.CompilerServices;

public class DocumentConverter
{
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
}