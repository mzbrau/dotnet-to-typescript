using SampleLibrary;
using SampleLibrary.Attributes;

namespace SampleLibrary2;

[JavascriptType]
[JavascriptObject("truck")]
public class Truck : IVehicle
{
    public string Make { get; set; }
    public string Model { get; set; }
    public int Year { get; set; }
    public double CargoCapacity { get; set; }
    public List<User> Drivers { get; set; } = new();
    public CargoManifest CurrentCargo { get; set; }
    public TruckStatus Status { get; private set; }
    private List<IMaintenanceRecord> _maintenanceHistory = new();

    public async Task<bool> StartEngineAsync()
    {
        await Task.Delay(1);
        return true;
    }

    public async Task<LoadingResult> LoadCargoAsync(CargoManifest cargo)
    {
        await Task.Delay(1);
        return new LoadingResult();
    }

    public List<IMaintenanceRecord> GetMaintenanceHistory()
    {
        return _maintenanceHistory;
    }

    public List<User> GetDrivers()
    {
        return Drivers;
    }

    public void SetDrivers(List<User> drivers)
    {
        Drivers = drivers;
    }
}