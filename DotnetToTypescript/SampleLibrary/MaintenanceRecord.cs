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
} 