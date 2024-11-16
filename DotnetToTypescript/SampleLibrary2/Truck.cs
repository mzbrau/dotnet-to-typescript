using SampleLibrary;

namespace SampleLibrary2;

[JavascriptType]
[JavascriptObject("truck")]
public class Truck
{
    public List<User> Drivers { get; set; }

    public List<User> GetDrivers()
    {
        return Drivers;
    }

    public void SetDrivers(List<User> drivers)
    {
        Drivers = drivers;
    }
}