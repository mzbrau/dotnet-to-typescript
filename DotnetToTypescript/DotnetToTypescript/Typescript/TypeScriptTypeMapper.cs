namespace DotnetToTypescript.Typescript;

public class TypeScriptTypeMapper
{
    private readonly Type? _javascriptTypeAttribute;

    public TypeScriptTypeMapper()
    {
        // Find the JavascriptTypeAttribute once during initialization
        _javascriptTypeAttribute = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == "JavascriptTypeAttribute");
    }

    public string MapToTypeScriptType(Type type)
    {
        if (type == typeof(void)) return "void";
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            return "number";
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return "number";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(string)) return "string";
        if (type == typeof(DateTime)) return "Date";
        
        // Handle arrays and lists
        if (type.IsArray)
        {
            return $"{MapToTypeScriptType(type.GetElementType())}[]";
        }
            
        if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>) || 
                                  type.GetGenericTypeDefinition() == typeof(IList<>) ||
                                  type.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            return $"{MapToTypeScriptType(type.GetGenericArguments()[0])}[]";

        // Handle Nullable<T>
        if (Nullable.GetUnderlyingType(type) != null)
            return MapToTypeScriptType(Nullable.GetUnderlyingType(type));

        // For interfaces, try to find concrete implementation with JavascriptType attribute
        if (type.IsInterface)
        {
            var concreteType = FindConcreteImplementation(type);
            return concreteType?.Name ?? type.Name;
        }

        return type.Name;
    }

    private Type? FindConcreteImplementation(Type interfaceType)
    {
        if (_javascriptTypeAttribute == null)
            return null;
        
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && interfaceType.IsAssignableFrom(t))
            .FirstOrDefault(t => t.GetCustomAttributes(_javascriptTypeAttribute, false).Any());
    }
}