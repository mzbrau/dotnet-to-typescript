using System.Reflection;
using System.Text;

namespace DotnetToTypescript.Typescript;

public class TypeScriptDefinitionGenerator : IDefinitionGenerator
{
    private readonly TypeScriptTypeMapper _typeMapper = new();

    public string GenerateDefinitions(IEnumerable<Type> scriptClasses)
    {
        var sb = new StringBuilder();
        var processedTypes = new HashSet<Type>();

        sb.AppendLine("// Class Definitions");
        foreach (var type in scriptClasses)
        {
            ProcessClass(type, sb, processedTypes);
        }
        
        var enumDefinitions = _typeMapper.GenerateEnumDefinitions();
        if (!string.IsNullOrEmpty(enumDefinitions))
        {
            sb.AppendLine("// Enum Definitions");
            sb.AppendLine(enumDefinitions);
        }

        return sb.ToString();
    }

    public string GenerateInstances(Dictionary<Type, string> scriptCreateNames, 
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
            var typeName = entry.Key.Name;
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

    private void ProcessClass(Type type, StringBuilder sb, HashSet<Type> processedTypes)
    {
        if (!processedTypes.Add(type)) 
            return;

        sb.AppendLine($"declare class {type.Name} {{");

        var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (var prop in properties)
        {
            sb.AppendLine($"    {prop.Name}: {_typeMapper.MapToTypeScriptType(prop.PropertyType)};");
        }

        var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName);
        foreach (var method in methods)
        {
            var parameters = method.GetParameters()
                .Select(p => $"{p.Name}: {_typeMapper.MapToTypeScriptType(p.ParameterType)}")
                .ToArray();

            sb.AppendLine($"    {method.Name}({string.Join(", ", parameters)}): {_typeMapper.MapToTypeScriptType(method.ReturnType)};");
        }

        sb.AppendLine("}");
    }

    private string GetDefaultValueForType(Type type)
    {
        if (type == typeof(string)) return "\"\"";
        if (type == typeof(bool)) return "false";
        if (type == typeof(DateTime)) return "new Date()";
        if (type.IsValueType) return "0";
        return "null";
    }
}