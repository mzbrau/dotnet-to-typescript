namespace DotnetToTypescript.IntegrationTests.Helpers
{
    public class TestFileHelper : IDisposable
    {
        public readonly List<string> CreatedFiles = new();
        
        public string GetTestFilePath(string fileName)
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, fileName);
            CreatedFiles.Add(directory);
            return path;
        }

        public void Dispose()
        {
            foreach (var path in CreatedFiles)
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
        }
    }
} 