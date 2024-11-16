using SampleLibrary.Attributes;

namespace SampleLibrary2;

[JavascriptType]
public class CargoManifest
{
    public string ManifestId { get; set; }
    public double Weight { get; set; }
    public List<CargoItem> Items { get; set; } = new();
    public IDictionary<string, string> CustomsDeclarations { get; set; } = new Dictionary<string, string>();
    public DateTime LoadingTime { get; set; }
    public bool? IsHazardous { get; set; }
} 