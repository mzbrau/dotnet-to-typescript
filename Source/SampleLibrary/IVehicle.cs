namespace SampleLibrary;

public interface IVehicle
{
    string Make { get; set; }
    string Model { get; set; }
    int Year { get; set; }
    Task<bool> StartEngineAsync();
    List<IMaintenanceRecord> GetMaintenanceHistory();
}