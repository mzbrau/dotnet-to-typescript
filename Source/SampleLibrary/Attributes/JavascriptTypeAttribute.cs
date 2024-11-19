namespace SampleLibrary.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Struct)]
public class JavascriptTypeAttribute : Attribute
{
    public JavascriptTypeAttribute(bool isSkipped = false)
    {
        SkipDefinition = isSkipped;
    }

    public bool SkipDefinition { get; }
}