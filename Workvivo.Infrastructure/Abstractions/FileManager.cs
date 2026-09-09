namespace Workvivo.Infrastructure.Abstractions;
public class FileManager : IFileManager
{
    public async Task<string> SaveFile(string root, string folderName, string fileName, IFormFile fileContent)
    {
        string filePath = Path.Combine(folderName, fileName);
        string filePathRoot = Path.Combine(root, folderName, fileName);
        CreateDirectory(Path.GetDirectoryName(filePathRoot));
        if (FileExists(filePathRoot))
        {
            DeleteFile(filePathRoot);
        }
        CreateFile(filePathRoot, await GetBytesFromFormFileAsync(fileContent));
        return filePath;
    }
    //public IFormFile ConvertSvgToPdfWithIronPdf(IFormFile svgFile)
    //{
    //    // Ensure the SVG file is not null and has content
    //    if (svgFile == null || svgFile.Length == 0)
    //        throw new ArgumentException("SVG file is null or empty.");

    //    // Read the SVG content
    //    string svgContent;
    //    using (var reader = new StreamReader(svgFile.OpenReadStream()))
    //    {
    //        svgContent = reader.ReadToEnd();
    //    }

    //    // Initialize the IronPdf renderer
    //    var renderer = new HtmlToPdf();

    //    // Convert SVG content to PDF (wrap SVG in basic HTML)
    //    var pdfDocument = renderer.RenderHtmlAsPdf($"<html><body>{svgContent}</body></html>");

    //    // Path for the temporary PDF file
    //    string tempFilePath = Path.GetTempFileName();
    //    try
    //    {
    //        // Save the PDF to a temporary file
    //        pdfDocument.SaveAs(tempFilePath);

    //        // Read the PDF file into a MemoryStream
    //        using (var memoryStream = new MemoryStream())
    //        {
    //            using (var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
    //            {
    //                fileStream.CopyTo(memoryStream);
    //            }

    //            // Reset the stream position to the beginning
    //            memoryStream.Position = 0;

    //            // Ensure the MemoryStream is not null and has content
    //            if (memoryStream.Length == 0)
    //                throw new InvalidOperationException("The PDF was not created successfully.");

    //            // Create IFormFile from MemoryStream
    //            var formFile = new FormFile(memoryStream, 0, memoryStream.Length, "pdf", "output.pdf")
    //            {
    //                Headers = new HeaderDictionary(),
    //                ContentType = "application/pdf"
    //            };

    //            return formFile;
    //        }
    //    }
    //    finally
    //    {
    //        // Clean up the temporary file
    //        if (File.Exists(tempFilePath))
    //        {
    //            File.Delete(tempFilePath);
    //        }
    //    }
    //}
    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public void DeleteDirectory(string path)
    {
        Directory.Delete(path, true);
    }

    public void CopyDirectory(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(destinationPath);

        foreach (string file in Directory.GetFiles(sourcePath))
        {
            string fileName = Path.GetFileName(file);
            string destinationFile = Path.Combine(destinationPath, fileName);
            File.Copy(file, destinationFile);
        }

        foreach (string directory in Directory.GetDirectories(sourcePath))
        {
            string directoryName = Path.GetFileName(directory);
            string destinationDirectory = Path.Combine(destinationPath, directoryName);
            CopyDirectory(directory, destinationDirectory);
        }
    }

    public void MoveDirectory(string sourcePath, string destinationPath)
    {
        Directory.Move(sourcePath, destinationPath);
    }

    public void CreateFile(string path, byte[] content)
    {
        File.WriteAllBytes(path, content);
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public byte[] ReadFile(string path)
    {
        return File.ReadAllBytes(path);
    }

    public void WriteFile(string path, byte[] content)
    {
        File.WriteAllBytes(path, content);
    }

    public void DeleteFile(string path)
    {
        File.Delete(path);
    }

    public void CopyFile(string sourcePath, string destinationPath)
    {
        File.Copy(sourcePath, destinationPath, true);
    }

    public void MoveFile(string sourcePath, string destinationPath)
    {
        File.Move(sourcePath, destinationPath);
    }
    private async Task<byte[]> GetBytesFromFormFileAsync(IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }
}
