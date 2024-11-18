using BenchmarkDotNet.Attributes;
using SampleLibrary;

namespace DotnetToJavascript.Benchmarks;

public class AttributePerformanceTests
{
    [Benchmark]
    public string? GetAttributeValueExtension()
    {
        return typeof(MaintenanceRecord).GetObjectName();
    }
}