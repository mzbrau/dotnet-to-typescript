using BenchmarkDotNet.Attributes;
using SampleLibrary;

namespace DotnetToTypescript.IntegrationTests;

public class AttributePerformanceTests
{
    [Benchmark]
    public string? GetAttributeValueExtension()
    {
        return typeof(MaintenanceRecord).GetObjectName();
    }
}