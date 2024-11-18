namespace DotnetToTypescript.Typescript;
using System.Text;

public class TypeScriptTypeMapper
{
    private readonly Type? _javascriptTypeAttribute;
    private readonly HashSet<Type> _discoveredEnums = new();
    private readonly HashSet<Type> _discoveredSystemTypes = new();

    public TypeScriptTypeMapper()
    {
        // Find the JavascriptTypeAttribute once during initialization
        _javascriptTypeAttribute = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == "JavascriptTypeAttribute");
    }

    public string MapToTypeScriptType(Type type)
    {
        // Track system types
        if (type.Namespace?.StartsWith("System") == true && 
            type != typeof(void) && 
            !type.IsPrimitive && 
            type != typeof(string) && 
            type != typeof(DateTime))
        {
            _discoveredSystemTypes.Add(type);
        }

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
        
        // Handle Func types
        if (IsFuncType(type))
        {
            return MapFuncToTypeScript(type);
        }
        
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
                
                if (valueType.IsInterface)
                {
                    var concreteType = FindFirstConcreteImplementation(valueType);
                    valueType = concreteType ?? valueType;
                }
                
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

    private bool IsFuncType(Type type)
    {
        if (!type.IsGenericType)
            return type == typeof(Func<>);
        
        var genericTypeDef = type.GetGenericTypeDefinition();
        return genericTypeDef.FullName?.StartsWith("System.Func`") == true;
    }

    private string MapFuncToTypeScript(Type funcType)
    {
        if (!funcType.IsGenericType)
            return "() => void";

        var genericArgs = funcType.GetGenericArguments();
        var returnType = genericArgs[^1]; // Last argument is return type
        var parameterTypes = genericArgs.Take(genericArgs.Length - 1).ToArray();

        var parameters = parameterTypes.Length > 0
            ? string.Join(", ", parameterTypes.Select((t, i) => $"arg{i}: {MapToTypeScriptType(t)}"))
            : "";

        return $"({parameters}) => {MapToTypeScriptType(returnType)}";
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

    public HashSet<Type> GetDiscoveredSystemTypes() => _discoveredSystemTypes;
}