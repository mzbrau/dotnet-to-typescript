namespace DotnetToTypescript.IO;

public class FileSystem : IFileSystem
{
    public Task WriteAllTextAsync(string path, string content) => 
        File.WriteAllTextAsync(path, content);

    public void CreateDirectory(string path) => 
        Directory.CreateDirectory(path);

    public string GetFileNameWithoutExtension(string path) => 
        Path.GetFileNameWithoutExtension(path);

    public string ChangeExtension(string path, string extension) => 
        Path.ChangeExtension(path, extension);

    public string Combine(params string[] paths) => 
        Path.Combine(paths);
}