namespace DotnetToTypescript;

public class TypeScriptTypeMapper
{
    public string MapToTypeScriptType(Type type)
    {
        if (type == typeof(void)) return "void";
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            return "number";
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return "number";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(string)) return "string";
        if (type == typeof(DateTime)) return "Date";
        if (type.IsArray) return $"{MapToTypeScriptType(type.GetElementType())}[]";

        // Handle Nullable<T> (e.g., int?)
        if (Nullable.GetUnderlyingType(type) != null) return MapToTypeScriptType(Nullable.GetUnderlyingType(type));

        // For user-defined types, return the class name
        if (type.IsClass || type.IsInterface) return type.Name;

        return "any";
    }
}