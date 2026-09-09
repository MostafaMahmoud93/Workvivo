namespace Workvivo.Domain.Abstractions.Interfaces;
public interface IFileManager
{
    Task<string> SaveFile(string root, string folderName, string fileName, IFormFile fileContent);
    void CopyDirectory(string sourcePath, string destinationPath);
    void MoveDirectory(string sourcePath, string destinationPath);
    void CopyFile(string sourcePath, string destinationPath);
    void MoveFile(string sourcePath, string destinationPath);
    void CreateFile(string path, byte[] content);
    void WriteFile(string path, byte[] content);
    void CreateDirectory(string path);
    bool DirectoryExists(string path);
    void DeleteDirectory(string path);
    bool FileExists(string path);
    byte[] ReadFile(string path);
    void DeleteFile(string path);
}
