namespace SampleLibrary;

public class ScriptCreateAttribute : Attribute
{
    public string Name { get; }

    public ScriptCreateAttribute(string name)
    {
        Name = name;
    }
}