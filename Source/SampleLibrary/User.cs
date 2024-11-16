using SampleLibrary.Attributes;

namespace SampleLibrary;

[JavascriptType]
[JavascriptObject("mike")]
public class User
{
    public string Name { get; set; }
    public string Address { get; set; }
    public DateTime DateOfBirth { get; set; }
    public List<string> PhoneNumbers { get; set; } = new();
    public Dictionary<string, bool> Permissions { get; set; } = new();
    public UserStatus Status { get; private set; }
    
    public async Task<bool> ValidateAsync()
    {
        await Task.Delay(100); // Simulating validation
        return !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Address);
    }

    public void AddPhoneNumber(string number)
    {
        PhoneNumbers.Add(number);
    }

    public void SetPermission(string permission, bool value)
    {
        Permissions[permission] = value;
    }

    public bool HasPermission(string permission)
    {
        return Permissions.TryGetValue(permission, out bool value) && value;
    }
}