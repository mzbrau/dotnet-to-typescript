using SampleLibrary.Attributes;

namespace SampleLibrary2;

[JavascriptType]
public class CargoItem
{
    public string ItemId { get; set; }
    public string? Description { get; set; }
    public double Weight { get; set; }
    public Dictionary<string, double> Dimensions { get; set; } = new();
    public bool RequiresRefrigeration { get; set; }
} 