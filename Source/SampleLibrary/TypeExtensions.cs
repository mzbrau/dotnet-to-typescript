using SampleLibrary.Attributes;

namespace SampleLibrary;

public static class TypeExtensions
{
    public static string? GetObjectName(this Type type)
    {
        return type.GetCustomAttributes(typeof(JavascriptObjectAttribute), inherit: false)
            .FirstOrDefault() is { } attribute
            ? (string?)typeof(JavascriptObjectAttribute).GetProperty(nameof(JavascriptObjectAttribute.Name))?.GetValue(attribute)
            : null;
    }
}