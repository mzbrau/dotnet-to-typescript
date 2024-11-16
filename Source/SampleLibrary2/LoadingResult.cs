using SampleLibrary.Attributes;

namespace SampleLibrary2;

[JavascriptType]
public class LoadingResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
} 