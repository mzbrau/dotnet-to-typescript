namespace SampleLibrary;

public class JavascriptObjectAttribute : Attribute
{
    public string Name { get; }

    public JavascriptObjectAttribute(string name)
    {
        Name = name;
    }
}