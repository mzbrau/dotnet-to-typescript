using System.Reflection;
using System.Text;

namespace DotnetToTypescript.Typescript;

public class TypeScriptDefinitionGenerator : IDefinitionGenerator
{
    private readonly TypeScriptTypeMapper _typeMapper = new();
    private readonly HashSet<Type> _systemTypes = new();
    private Type? _typeAttribute;

    public void Initialize(Type? typeAttribute)
    {
        _typeAttribute = typeAttribute;
    }

    public string GenerateDefinitions(IEnumerable<Type> scriptClasses, bool preserveCase = false)
    {
        var sb = new StringBuilder();
        var processedTypes = new HashSet<Type>();

        sb.AppendLine("// Class Definitions");
        foreach (var type in scriptClasses)
        {
            ProcessClass(type, sb, processedTypes, preserveCase);
        }
        
        var enumDefinitions = _typeMapper.GenerateEnumDefinitions();
        if (!string.IsNullOrEmpty(enumDefinitions))
        {
            sb.AppendLine("// Enum Definitions");
            sb.AppendLine(enumDefinitions);
        }

        var systemTypeDefinitions = GenerateSystemTypeDefinitions();
        if (!string.IsNullOrEmpty(systemTypeDefinitions))
        {
            sb.AppendLine("// System Type Definitions");
            sb.AppendLine(systemTypeDefinitions);
        }

        return sb.ToString();
    }

    public string GenerateInstances(
        Dictionary<(Type Type, string InstanceName), string> scriptCreateNames,
        Dictionary<(Type Type, string PropertyName), string> scriptPropertyNames,
        string definitionPath)
    {
        var sb = new StringBuilder();
        
        // Add reference to definition file
        var definitionFile = Path.GetFileName(definitionPath);
        sb.AppendLine($"/// <reference path=\"{Path.GetFileName(definitionFile)}\" />");
        sb.AppendLine();

        // Generate class instances
        foreach (var entry in scriptCreateNames)
        {
            var typeName = entry.Key.Type.Name;
            var instanceName = entry.Value;
            sb.AppendLine($"let {instanceName} = new {typeName}();");
        }

        // Generate property instances
        foreach (var entry in scriptPropertyNames)
        {
            var propertyType = entry.Key.Type;
            var propertyInfo = propertyType.GetProperty(entry.Key.PropertyName);
            if (propertyInfo != null)
            {
                var defaultValue = GetDefaultValueForType(propertyInfo.PropertyType);
                sb.AppendLine($"let {entry.Value} = {defaultValue};");
            }
        }
        
        sb.AppendLine();
        sb.AppendLine("// Insert your script below");
        sb.AppendLine("// -------------------------");
        sb.AppendLine();

        return sb.ToString();
    }

    private void ProcessClass(Type type, StringBuilder sb, HashSet<Type> processedTypes, bool preserveCase)
    {
        if (!processedTypes.Add(type)) 
            return;

        // Track system types for the class itself
        if (type.Namespace?.StartsWith("System") == true)
        {
            _systemTypes.Add(type);
            return;
        }

        sb.AppendLine($"declare class {type.Name} {{");

        // Process properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (var prop in properties)
        {
            // Track system types from property types
            TrackSystemType(prop.PropertyType);
            sb.AppendLine($"    {FormatName(prop.Name, preserveCase)}: {_typeMapper.MapToTypeScriptType(prop.PropertyType)};");
        }

        // Process methods
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName);
        foreach (var method in methods)
        {
            // Track system types from return type
            TrackSystemType(method.ReturnType);

            // Track system types from parameters
            foreach (var parameter in method.GetParameters())
            {
                TrackSystemType(parameter.ParameterType);
            }

            var parameters = method.GetParameters()
                .Select(p => $"{FormatName(p.Name!, preserveCase)}: {_typeMapper.MapToTypeScriptType(p.ParameterType)}")
                .ToArray();

            sb.AppendLine($"    {FormatName(method.Name, preserveCase)}({string.Join(", ", parameters)}): {_typeMapper.MapToTypeScriptType(method.ReturnType)};");
        }

        sb.AppendLine("}");
        sb.AppendLine();

        // Process nested classes
        var nestedTypes = type.GetNestedTypes(BindingFlags.Public)
            .Where(t => t.IsClass && _typeAttribute != null && t.GetCustomAttributes(_typeAttribute, false).Length != 0);
            
        foreach (var nestedType in nestedTypes)
        {
            ProcessClass(nestedType, sb, processedTypes, preserveCase);
        }
    }

    private void TrackSystemType(Type type)
    {
        // Handle array types
        if (type.IsArray)
        {
            TrackSystemType(type.GetElementType()!);
            return;
        }

        // Handle generic types
        if (type.IsGenericType)
        {
            foreach (var genericArg in type.GetGenericArguments())
            {
                TrackSystemType(genericArg);
            }

            // Also track the generic type definition if it's from System namespace
            var genericTypeDef = type.GetGenericTypeDefinition();
            if (genericTypeDef.Namespace?.StartsWith("System") == true)
            {
                _systemTypes.Add(genericTypeDef);
            }
        }

        // Track the type itself if it's from System namespace
        if (type.Namespace?.StartsWith("System") == true &&
            type != typeof(void) &&
            !type.IsPrimitive &&
            type != typeof(string) &&
            type != typeof(DateTime))
        {
            _systemTypes.Add(type);
        }
    }

    private string GetDefaultValueForType(Type type)
    {
        if (type == typeof(string)) return "\"\"";
        if (type == typeof(bool)) return "false";
        if (type == typeof(DateTime)) return "new Date()";
        if (type.IsValueType) return "0";
        return "null";
    }

    private string FormatName(string name, bool preserveCase)
    {
        if (preserveCase)
            return name;
        
        return char.ToLower(name[0]) + name.Substring(1);
    }

    private string GenerateSystemTypeDefinitions()
    {
        var sb = new StringBuilder();
        
        foreach (var type in _systemTypes)
        {
            if (type == typeof(Exception))
            {
                sb.AppendLine("declare class Exception {");
                sb.AppendLine("    message: string;");
                sb.AppendLine("    stackTrace?: string;");
                sb.AppendLine("    innerException?: Exception;");
                sb.AppendLine("}");
                sb.AppendLine();
            }
            else if (type == typeof(ArgumentException))
            {
                sb.AppendLine("declare class ArgumentException extends Exception {");
                sb.AppendLine("    paramName?: string;");
                sb.AppendLine("}");
                sb.AppendLine();
            }
            // Add more system types as needed
        }

        return sb.ToString();
    }
}