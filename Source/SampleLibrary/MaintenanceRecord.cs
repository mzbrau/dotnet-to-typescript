using SampleLibrary.Attributes;

namespace SampleLibrary;

[JavascriptType]
[JavascriptObject("MaintenanceRecord")]
public class MaintenanceRecord : IMaintenanceRecord
{
    public DateTime ServiceDate { get; set; }
    public string Description { get; set; }
    public decimal Cost { get; set; }
    public string Mechanic { get; set; }
    public List<string> PartsReplaced { get; set; } = new();
    public bool IsWarrantyWork { get; set; }

    [JavascriptObject("maxRetries")]
    public int MaxRetries { get; set; }

    [JavascriptObject("apiKey")]
    public string ApiKey { get; set; }

    [JavascriptObject("isEnabled")]
    public bool IsEnabled { get; set; }
} 