namespace DotnetToTypescript.Typescript;
using System.Text;

public class TypeScriptTypeMapper
{
    private readonly Type? _javascriptTypeAttribute;
    private readonly HashSet<Type> _discoveredEnums = new();

    public TypeScriptTypeMapper()
    {
        // Find the JavascriptTypeAttribute once during initialization
        _javascriptTypeAttribute = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == "JavascriptTypeAttribute");
    }

    public string MapToTypeScriptType(Type type)
    {
        // Handle nullable types first
        if (Nullable.GetUnderlyingType(type) is Type underlyingType)
        {
            return $"{MapToTypeScriptType(underlyingType)} | null";
        }

        if (type == typeof(void)) return "void";
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            return "number";
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return "number";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(string)) return "string";
        if (type == typeof(DateTime)) return "Date";
        
        // Updated enum support
        if (type.IsEnum)
        {
            _discoveredEnums.Add(type);
            return type.Name;
        }
        
        // Add struct support
        if (type.IsValueType && !type.IsPrimitive && !type.IsEnum)
            return type.Name; // Treat structs like classes, using their name
        
        // Handle arrays and lists
        if (type.IsArray)
        {
            return $"{MapToTypeScriptType(type.GetElementType()!)}[]";
        }
            
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            
            // Handle Task<T>
            if (genericTypeDef == typeof(Task<>))
            {
                var taskType = type.GetGenericArguments()[0];
                return $"Promise<{MapToTypeScriptType(taskType)}>";
            }
            
            // Handle Lists and IEnumerables
            if (genericTypeDef == typeof(List<>) || 
                genericTypeDef == typeof(IList<>) ||
                genericTypeDef == typeof(IEnumerable<>))
            {
                var elementType = type.GetGenericArguments()[0];
                // If element type is an interface, try to find concrete implementation
                if (elementType.IsInterface)
                {
                    var concreteType = FindFirstConcreteImplementation(elementType);
                    elementType = concreteType ?? elementType;
                }
                return $"{MapToTypeScriptType(elementType)}[]";
            }
            
            // Handle Dictionaries
            if (genericTypeDef == typeof(Dictionary<,>) || 
                genericTypeDef == typeof(IDictionary<,>))
            {
                var keyType = type.GetGenericArguments()[0];
                var valueType = type.GetGenericArguments()[1];
                
                // If value type is an interface, try to find concrete implementation
                if (valueType.IsInterface)
                {
                    var concreteType = FindFirstConcreteImplementation(valueType);
                    valueType = concreteType ?? valueType;
                }
                
                // TypeScript only supports string or number as index signature types
                var mappedKeyType = keyType == typeof(string) ? "string" : "number";
                var mappedValueType = MapToTypeScriptType(valueType);
                
                return $"{{ [key: {mappedKeyType}]: {mappedValueType} }}";
            }
        }

        // Handle non-generic Task
        if (type == typeof(Task))
            return "Promise<void>";

        // For interfaces, try to find first concrete implementation
        if (type.IsInterface)
        {
            var concreteType = FindFirstConcreteImplementation(type);
            return concreteType?.Name ?? type.Name;
        }

        return type.Name;
    }

    private Type? FindFirstConcreteImplementation(Type interfaceType)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && interfaceType.IsAssignableFrom(t))
            .FirstOrDefault();
    }

    // Add new method to generate enum definitions
    public string GenerateEnumDefinitions()
    {
        var builder = new StringBuilder();
        
        foreach (var enumType in _discoveredEnums)
        {
            builder.AppendLine($"declare enum {enumType.Name} {{");
            
            foreach (var name in Enum.GetNames(enumType))
            {
                var value = Convert.ToInt32(Enum.Parse(enumType, name));
                builder.AppendLine($"    {name} = {value},");
            }
            
            builder.AppendLine("}");
            builder.AppendLine();
        }
        
        return builder.ToString();
    }
}