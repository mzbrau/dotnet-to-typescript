namespace DotnetToTypescript.IO;

public interface IFileSystem
{
    Task WriteAllTextAsync(string path, string content);
    void CreateDirectory(string path);
    string GetFileNameWithoutExtension(string path);
    string GetDirectoryName(string path);
    string ChangeExtension(string path, string extension);
    string Combine(params string[] paths);
}