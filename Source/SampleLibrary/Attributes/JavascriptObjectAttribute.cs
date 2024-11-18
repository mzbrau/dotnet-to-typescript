namespace SampleLibrary.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public class JavascriptObjectAttribute : Attribute
{
    public string Name { get; }

    public JavascriptObjectAttribute(string name)
    {
        Name = name;
    }
}