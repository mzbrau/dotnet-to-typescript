namespace SampleLibrary;

public interface IMaintenanceRecord
{
    DateTime ServiceDate { get; set; }
    string Description { get; set; }
    decimal Cost { get; set; }
}