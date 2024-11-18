using SampleLibrary.Attributes;

namespace SampleLibrary;

[JavascriptType]
public class Car : IVehicle
{
    private readonly List<IMaintenanceRecord> _maintenanceHistory = new();
    
    public string Make { get; set; }
    public string Model { get; set; }
    public int Year { get; set; }
    public double FuelLevel { get; set; }
    public bool IsRunning { get; private set; }
    public List<User> AuthorizedDrivers { get; set; } = new();
    public Dictionary<string, decimal> ServiceCosts { get; set; } = new();
    public CarType Type { get; set; }
    

    public async Task<bool> StartEngineAsync()
    {
        if (FuelLevel > 0)
        {
            await Task.Delay(1000); // Simulating engine start
            IsRunning = true;
            return true;
        }
        return false;
    }

    public async Task<double> RefuelAsync(double amount)
    {
        await Task.Delay(500); // Simulating refueling
        FuelLevel += amount;
        return FuelLevel;
    }

    public bool AddUser(User user)
    {
        if (user != null && !AuthorizedDrivers.Contains(user))
        {
            AuthorizedDrivers.Add(user);
            return true;
        }
        return false;
    }

    public List<IMaintenanceRecord> GetMaintenanceHistory()
    {
        return _maintenanceHistory;
    }

    public void AddMaintenanceRecord(MaintenanceRecord record)
    {
        _maintenanceHistory.Add(record);
        ServiceCosts[record.Description] = record.Cost;
    }

    public void LogError(Exception exception, DateTime timestamp)
    {
        Console.WriteLine($"Error: {exception.Message} at {timestamp}");
    }
}
